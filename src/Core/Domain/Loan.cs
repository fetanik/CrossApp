namespace Core.Domain;

public sealed class Loan
{
    public string Id { get; }
    public string BookCopyId { get; }
    public string ReaderId { get; }
    public DateTime IssuedOn { get; }

    public DateTime? ReturnedOn { get; private set; }

    public bool IsClosed =>
        ReturnedOn.HasValue;

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
        if (IsClosed)
        {
            throw new InvalidOperationException(
                $"Видача {Id} вже закрита");
        }

        if (returnedOn < IssuedOn)
        {
            throw new ArgumentOutOfRangeException(
                nameof(returnedOn),
                returnedOn,
                "Дата повернення не може бути раніше дати видачі");
        }

        ReturnedOn = returnedOn;
    }

    public override string ToString()
    {
        string status = IsClosed
            ? $"повернено {ReturnedOn:yyyy-MM-dd}"
            : "книга ще у читача";

        return $"{Id}: книга {BookCopyId}, " +
               $"читач {ReaderId}, " +
               $"видано {IssuedOn:yyyy-MM-dd} — {status}";
    }
}