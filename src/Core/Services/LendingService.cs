using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class LendingService
{
    private readonly ILibraryStore _store;

    public LendingService(
        ILibraryStore store)
    {
        _store = store
            ?? throw new ArgumentNullException(
                nameof(store));
    }

    public BookCopy AddBook(
        string isbn,
        string title,
        int year,
        string? author = null)
    {
        string id =
            Guid.NewGuid()
                .ToString("N")[..8];

        BookCopy copy =
            BookCopy.Create(
                id,
                isbn,
                title,
                year,
                author);

        _store.AddCopy(copy);

        return copy;
    }

    public Loan IssueCopy(
        string copyId,
        string readerId)
    {
        BookCopy copy =
            _store.GetCopyById(copyId)
            ?? throw new InvalidOperationException(
                $"Примірник з id={copyId} не знайдено.");

        IReadOnlyList<Loan> existingLoans =
            _store.ListLoans();

        Loan loan =
            LoanService.IssueBook(
                Guid.NewGuid()
                    .ToString("N")[..8],
                copy,
                readerId,
                DateTime.Now,
                existingLoans);

        _store.UpdateCopy(copy);
        _store.AddLoan(loan);

        return loan;
    }

    public void ReturnCopy(
        string loanId)
    {
        Loan loan =
            _store.GetLoanById(loanId)
            ?? throw new InvalidOperationException(
                $"Видачу з id={loanId} не знайдено.");

        BookCopy copy =
            _store.GetCopyById(
                loan.BookCopyId)
            ?? throw new InvalidOperationException(
                $"Примірник з id={loan.BookCopyId} " +
                "не знайдено.");

        loan.Close(
            DateTime.Now);

        copy.Return();

        _store.UpdateLoan(loan);
        _store.UpdateCopy(copy);
    }

    public IReadOnlyList<BookCopy> AllBooks() =>
        _store.ListCopies();

    public IReadOnlyList<Loan> AllLoans() =>
        _store.ListLoans();

    public BookCopy? FindBook(
        string id) =>
        _store.GetCopyById(id);

    public IReadOnlyList<BookCopy> SearchBooks(
        Func<BookCopy, bool> predicate) =>
        _store.FindCopies(predicate);
}