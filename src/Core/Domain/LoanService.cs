namespace Core.Domain;

public static class LoanService
{
    private const int MaxOpenLoans = 5;

    public static Loan IssueBook(
        string loanId,
        BookCopy book,
        string readerId,
        DateTime issuedOn,
        IReadOnlyList<Loan> existingLoans)
    {
        if (book.IsIssued)
        {
            throw new InvalidOperationException(
                $"Примірник {book.Id} вже виданий");
        }

        int openLoans = existingLoans.Count(
            loan =>
                loan.ReaderId == readerId &&
                !loan.IsClosed);

        if (openLoans >= MaxOpenLoans)
        {
            throw new InvalidOperationException(
                $"Читач {readerId} вже має " +
                $"{openLoans} відкритих видач. " +
                $"Максимум дозволено {MaxOpenLoans}");
        }

        Loan loan = Loan.Open(
            loanId,
            book.Id,
            readerId,
            issuedOn);

        book.Issue();

        return loan;
    }
}