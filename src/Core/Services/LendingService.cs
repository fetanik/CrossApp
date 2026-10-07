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

        int openLoans =
            _store.ListLoans()
                .Count(
                    loan =>
                        loan.ReaderId.Equals(
                            readerId,
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        loan.Status == LoanStatus.Open);

        if (openLoans >= 5)
        {
            throw new InvalidOperationException(
                $"Читач {readerId} уже має " +
                "5 відкритих видач.");
        }

        copy.Issue();

        Loan loan =
            Loan.Open(
                Guid.NewGuid()
                    .ToString("N")[..8],
                copy.Id,
                readerId,
                DateTime.Now);

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

    public BookCopy? FindBook(string id) =>
        _store.GetCopyById(id);
}