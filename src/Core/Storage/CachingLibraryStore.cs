using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingLibraryStore : ILibraryStore
{
    private readonly ILibraryStore _inner;
    private readonly Action<string>? _log;

    private readonly Dictionary<string, BookCopy> _copies =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Loan> _loans =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _copiesLoaded;
    private bool _loansLoaded;

    public CachingLibraryStore(
        ILibraryStore inner,
        Action<string>? log = null)
    {
        _inner = inner
            ?? throw new ArgumentNullException(nameof(inner));

        _log = log;
    }

    private void EnsureCopiesLoaded()
    {
        if (_copiesLoaded)
        {
            _log?.Invoke(
                "[Cache] Використання кешу книг.");

            return;
        }

        _log?.Invoke(
            "[Cache] Завантаження книг із внутрішнього сховища.");

        foreach (BookCopy copy in _inner.ListCopies())
        {
            _copies[copy.Id] = copy;
        }

        _copiesLoaded = true;
    }

    private void EnsureLoansLoaded()
    {
        if (_loansLoaded)
        {
            _log?.Invoke(
                "[Cache] Використання кешу видач.");

            return;
        }

        _log?.Invoke(
            "[Cache] Завантаження видач із внутрішнього сховища.");

        foreach (Loan loan in _inner.ListLoans())
        {
            _loans[loan.Id] = loan;
        }

        _loansLoaded = true;
    }

    public IReadOnlyList<BookCopy> ListCopies()
    {
        EnsureCopiesLoaded();

        return _copies.Values.ToList();
    }

    public BookCopy? GetCopyById(string id)
    {
        EnsureCopiesLoaded();

        return _copies.GetValueOrDefault(id);
    }

    public IReadOnlyList<BookCopy> FindCopies(
        Func<BookCopy, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        EnsureCopiesLoaded();

        return _copies.Values
            .Where(predicate)
            .ToList();
    }

    public void AddCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        _inner.AddCopy(copy);

        if (_copiesLoaded)
        {
            _copies.Add(copy.Id, copy);

            _log?.Invoke(
                $"[Cache] Додано примірник {copy.Id} у кеш.");
        }
    }

    public void UpdateCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        _inner.UpdateCopy(copy);

        if (_copiesLoaded)
        {
            _copies[copy.Id] = copy;

            _log?.Invoke(
                $"[Cache] Оновлено примірник {copy.Id} у кеші.");
        }
    }

    public IReadOnlyList<Loan> ListLoans()
    {
        EnsureLoansLoaded();

        return _loans.Values.ToList();
    }

    public Loan? GetLoanById(string id)
    {
        EnsureLoansLoaded();

        return _loans.GetValueOrDefault(id);
    }

    public void AddLoan(Loan loan)
    {
        ArgumentNullException.ThrowIfNull(loan);

        _inner.AddLoan(loan);

        if (_loansLoaded)
        {
            _loans.Add(loan.Id, loan);

            _log?.Invoke(
                $"[Cache] Додано видачу {loan.Id} у кеш.");
        }
    }

    public void UpdateLoan(Loan loan)
    {
        ArgumentNullException.ThrowIfNull(loan);

        _inner.UpdateLoan(loan);

        if (_loansLoaded)
        {
            _loans[loan.Id] = loan;

            _log?.Invoke(
                $"[Cache] Оновлено видачу {loan.Id} у кеші.");
        }
    }
}