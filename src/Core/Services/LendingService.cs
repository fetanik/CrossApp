using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Exceptions;
using Core.Import;

namespace Core.Services;

public sealed class LendingService
{
    private readonly ILibraryStore _store;
    private readonly INotificationSink? _notificationSink;

    public LendingService(
        ILibraryStore store,
        INotificationSink? notificationSink = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _notificationSink = notificationSink;
    }

    public event EventHandler<LibraryChangedEventArgs>? Changed;

    private void OnChanged(
        LibraryChangeKind kind,
        string copyId,
        string? readerId = null,
        DateTime? issuedOn = null)
    {
        var args = new LibraryChangedEventArgs(kind, copyId, readerId, issuedOn);

        Changed?.Invoke(this, args);

        _notificationSink?.Notify(args);
    }

    public BookCopy AddBook(
        string isbn,
        string title,
        int year,
        string? author = null)
    {
        string id = Guid.NewGuid().ToString("N")[..8];
        BookCopy copy = BookCopy.Create(id, isbn, title, year, author);

        _store.AddCopy(copy);
        OnChanged(LibraryChangeKind.BookAdded, copy.Id);
        return copy;
    }

    public Loan IssueCopy(string copyId, string readerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(copyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(readerId);

        BookCopy copy = _store.GetCopyById(copyId)
            ?? throw new DomainException(
                $"Примірник з id={copyId} не знайдено.");

        IReadOnlyList<Loan> existingLoans = _store.ListLoans();
        Loan loan = LoanService.IssueBook(
            Guid.NewGuid().ToString("N")[..8],
            copy,
            readerId,
            DateTime.Now,
            existingLoans);

        _store.UpdateCopy(copy);
        _store.AddLoan(loan);

        OnChanged(
            LibraryChangeKind.CopyIssued,
            loan.BookCopyId,
            loan.ReaderId,
            loan.IssuedOn);

        return loan;
    }

    public void ReturnCopy(string loanId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loanId);

        Loan loan = _store.GetLoanById(loanId)
            ?? throw new DomainException(
                $"Видачу з id={loanId} не знайдено.");

        BookCopy copy = _store.GetCopyById(loan.BookCopyId)
            ?? throw new DomainException(
                $"Примірник з id={loan.BookCopyId} не знайдено.");

        if (!copy.IsIssued)
        {
            throw new DomainException(
                $"Примірник {copy.Id} не виданий, тому його неможливо повернути");
        }

        if (loan.Status != LoanStatus.Open)
        {
            throw new DomainException(
                $"Перехід зі стану {loan.Status} " +
                $"у стан {LoanStatus.Closed} неможливий");
        }

        DateTime returnedOn = DateTime.Now;
        if (returnedOn < loan.IssuedOn)
        {
            throw new ArgumentOutOfRangeException(
                nameof(returnedOn),
                returnedOn,
                "Дата повернення не може бути раніше дати видачі");
        }

        loan.Close(returnedOn);
        copy.Return();

        _store.UpdateLoan(loan);
        _store.UpdateCopy(copy);

        OnChanged(
            LibraryChangeKind.CopyReturned,
            loan.BookCopyId,
            loan.ReaderId);
    }

    public async Task<int> ImportAsync(
        ICatalogImporter importer,
        string path,
        CancellationToken ct = default,
        IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<BookCopy> records = await importer.LoadAsync(path, ct);
        return AddImportedCopies(records, ct, progress);
    }

    public async Task<int> ImportStreamAsync(
        ICatalogStreamImporter importer,
        string path,
        CancellationToken ct = default,
        IProgress<int>? processed = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();

        // Під час потокового читання загальна кількість ще невідома,
        // тому progress повідомляє число вже прочитаних записів, а не відсоток.
        int added = 0;
        int seen = 0;

        await foreach (BookCopy copy in importer.ReadAsync(path, ct))
        {
            ct.ThrowIfCancellationRequested();
            seen++;
            processed?.Report(seen);

            if (TryAddCopy(copy))
            {
                added++;
            }
        }

        return added;
    }

    private int AddImportedCopies(
        IReadOnlyList<BookCopy> records,
        CancellationToken ct,
        IProgress<int>? progress)
    {
        int total = records.Count;
        progress?.Report(0);

        if (total == 0)
        {
            progress?.Report(100);
            return 0;
        }

        int added = 0;

        for (int i = 0; i < records.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            if (TryAddCopy(records[i]))
            {
                added++;
            }

            int processed = i + 1;
            int percent = processed == total
                ? 100
                : processed * 100 / total;
            progress?.Report(percent);
        }

        return added;
    }

    private bool TryAddCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        if (_store.GetCopyById(copy.Id) is not null)
        {
            return false;
        }

        _store.AddCopy(copy);
        OnChanged(LibraryChangeKind.BookAdded, copy.Id);
        return true;
    }

    public IReadOnlyList<BookCopy> AllBooks() => _store.ListCopies();

    public IReadOnlyList<Loan> AllLoans() => _store.ListLoans();

    public BookCopy? FindBook(string id) => _store.GetCopyById(id);

    public IReadOnlyList<BookCopy> SearchBooks(Func<BookCopy, bool> predicate) =>
        _store.FindCopies(predicate);
}
