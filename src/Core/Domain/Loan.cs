namespace Core.Domain;

public sealed class Loan
{
    public string Id { get; }
    public string BookCopyId { get; }
    public string ReaderId { get; }
    public DateTime IssuedOn { get; }

    public DateTime? ReturnedOn { get; private set; }

    public LoanStatus Status { get; private set; }

    public bool IsClosed =>
        Status == LoanStatus.Closed;

    private Loan(
        string id,
        string bookCopyId,
        string readerId,
        DateTime issuedOn)
    {
        Id = id;
        BookCopyId = bookCopyId;
        ReaderId = readerId;
        IssuedOn = issuedOn;

        ReturnedOn = null;
        Status = LoanStatus.Open;
    }

    public static Loan Open(
        string id,
        string bookCopyId,
        string readerId,
        DateTime issuedOn)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Ідентифікатор видачі не може бути порожнім",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(bookCopyId))
        {
            throw new ArgumentException(
                "Ідентифікатор примірника книги не може бути порожнім",
                nameof(bookCopyId));
        }

        if (string.IsNullOrWhiteSpace(readerId))
        {
            throw new ArgumentException(
                "Ідентифікатор читача не може бути порожнім",
                nameof(readerId));
        }

        return new Loan(
            id.Trim(),
            bookCopyId.Trim(),
            readerId.Trim(),
            issuedOn);
    }

    public void Close(DateTime returnedOn)
    {
        EnsureTransitionAllowed(
            LoanStatus.Closed);

        if (returnedOn < IssuedOn)
        {
            throw new ArgumentOutOfRangeException(
                nameof(returnedOn),
                returnedOn,
                "Дата повернення не може бути раніше дати видачі");
        }

        ReturnedOn = returnedOn;
        Status = LoanStatus.Closed;
    }

    public void Cancel()
    {
        EnsureTransitionAllowed(
            LoanStatus.Cancelled);

        Status = LoanStatus.Cancelled;
    }

    private void EnsureTransitionAllowed(
        LoanStatus newStatus)
    {
        bool allowed =
            (Status, newStatus) switch
            {
                (LoanStatus.Open,
                 LoanStatus.Closed) => true,

                (LoanStatus.Open,
                 LoanStatus.Cancelled) => true,

                _ => false
            };

        if (!allowed)
        {
            throw new InvalidOperationException(
                $"Перехід зі стану {Status} " +
                $"у стан {newStatus} неможливий");
        }
    }

    public override string ToString()
    {
        string status = Status switch
        {
            LoanStatus.Open =>
                "книга ще у читача",

            LoanStatus.Closed =>
                $"повернено {ReturnedOn:yyyy-MM-dd}",

            LoanStatus.Cancelled =>
                "видачу скасовано",

            _ =>
                "невідомий стан"
        };

        return $"{Id}: книга {BookCopyId}, " +
               $"читач {ReaderId}, " +
               $"видано {IssuedOn:yyyy-MM-dd} — {status}";
    }
}