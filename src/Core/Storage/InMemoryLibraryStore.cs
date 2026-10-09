using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemoryLibraryStore : ILibraryStore
{
    private readonly Dictionary<string, BookCopy> _copies;
    private readonly Dictionary<string, Loan> _loans;

    public InMemoryLibraryStore(
        IEnumerable<BookCopy>? copies = null,
        IEnumerable<Loan>? loans = null)
    {
        _copies = (copies ?? [])
            .ToDictionary(
                copy => copy.Id,
                StringComparer.OrdinalIgnoreCase);

        _loans = (loans ?? [])
            .ToDictionary(
                loan => loan.Id,
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<BookCopy> ListCopies() =>
        _copies.Values.ToList();

    public BookCopy? GetCopyById(string id) =>
        _copies.GetValueOrDefault(id);

    public IReadOnlyList<BookCopy> FindCopies(
        Func<BookCopy, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return _copies.Values
            .Where(predicate)
            .ToList();
    }

    public void AddCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        if (_copies.ContainsKey(copy.Id))
        {
            throw new InvalidOperationException(
                $"Примірник з id={copy.Id} уже існує.");
        }

        _copies.Add(copy.Id, copy);
    }

    public void UpdateCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        _copies[copy.Id] = copy;
    }

    public IReadOnlyList<Loan> ListLoans() =>
        _loans.Values.ToList();

    public Loan? GetLoanById(string id) =>
        _loans.GetValueOrDefault(id);

    public void AddLoan(Loan loan)
    {
        ArgumentNullException.ThrowIfNull(loan);

        if (_loans.ContainsKey(loan.Id))
        {
            throw new InvalidOperationException(
                $"Видача з id={loan.Id} уже існує.");
        }

        _loans.Add(loan.Id, loan);
    }

    public void UpdateLoan(Loan loan)
    {
        ArgumentNullException.ThrowIfNull(loan);

        _loans[loan.Id] = loan;
    }
}