namespace Core.Domain;

public sealed class Reader
{
    private readonly List<Loan> _loans = [];

    public string Id { get; }
    public string FullName { get; }

    public IReadOnlyList<Loan> Loans =>
        _loans.AsReadOnly();

    private Reader(
        string id,
        string fullName)
    {
        Id = id;
        FullName = fullName;
    }

    public static Reader Create(
        string id,
        string fullName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Ідентифікатор читача не може бути порожнім",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(
                "Ім'я читача не може бути порожнім",
                nameof(fullName));
        }

        return new Reader(
            id.Trim(),
            fullName.Trim());
    }

    public void AddLoan(Loan loan)
    {
        if (loan is null)
        {
            throw new ArgumentNullException(
                nameof(loan));
        }

        _loans.Add(loan);
    }
}