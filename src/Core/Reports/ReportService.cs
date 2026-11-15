using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    // У доменній моделі немає DueDate і немає правила строку повернення.
    // Для звіту прострочених видач прийнято: книга має бути повернена
    // протягом 14 днів від IssuedOn. Формат збереження Loan не змінюється.
    public const int LoanPeriodDays = 14;

    public static IReadOnlyList<TopReaderLine> TopReaders(
        IEnumerable<Loan> loans)
    {
        ArgumentNullException.ThrowIfNull(loans);

        return loans
            .Where(loan => loan.Status != LoanStatus.Cancelled)
            .GroupBy(loan => loan.ReaderId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TopReaderLine(
                group.Key,
                group.Count()))
            .TopBy(
                line => line.LoanCount,
                5,
                line => line.ReaderId,
                StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<TopCopyLine> TopBorrowedCopies(
        IEnumerable<BookCopy> copies,
        IEnumerable<Loan> loans)
    {
        ArgumentNullException.ThrowIfNull(copies);
        ArgumentNullException.ThrowIfNull(loans);

        Dictionary<string, int> loanCounts = loans
            .Where(loan => loan.Status != LoanStatus.Cancelled)
            .GroupBy(loan => loan.BookCopyId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);

        return copies
            .Select(copy => new TopCopyLine(
                copy.Id,
                copy.Isbn,
                loanCounts.GetValueOrDefault(copy.Id)))
            .TopBy(
                line => line.LoanCount,
                5,
                line => line.CopyId,
                StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<CopyLoanSummary> CopyLoans(
        IEnumerable<BookCopy> copies,
        IEnumerable<Loan> loans)
    {
        ArgumentNullException.ThrowIfNull(copies);
        ArgumentNullException.ThrowIfNull(loans);

        return copies
            .GroupJoin(
                loans.Where(loan => loan.Status != LoanStatus.Cancelled),
                copy => copy.Id,
                loan => loan.BookCopyId,
                (copy, copyLoans) => new CopyLoanSummary(
                    copy.Id,
                    copy.Isbn,
                    copyLoans.Count(),
                    copyLoans.Count(loan => loan.Status == LoanStatus.Open)),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(line => line.CopyId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<OverdueLoanLine> OverdueLoans(
        IEnumerable<Loan> loans,
        DateTime asOf)
    {
        ArgumentNullException.ThrowIfNull(loans);

        return loans
            .Where(loan =>
                loan.Status == LoanStatus.Open &&
                asOf.Date > loan.IssuedOn.Date.AddDays(LoanPeriodDays))
            .OrderBy(loan => loan.IssuedOn)
            .ThenBy(loan => loan.Id, StringComparer.Ordinal)
            .Select(loan => new OverdueLoanLine(
                loan.Id,
                loan.BookCopyId,
                loan.ReaderId,
                loan.IssuedOn,
                loan.IssuedOn.Date.AddDays(LoanPeriodDays),
                (asOf.Date - loan.IssuedOn.Date.AddDays(LoanPeriodDays)).Days))
            .ToList();
    }

    public static IReadOnlyList<YearGroupSummary> ByYear(
        IEnumerable<BookCopy> copies)
    {
        ArgumentNullException.ThrowIfNull(copies);

        return copies
            .GroupBy(copy => copy.Year)
            .Select(group => new YearGroupSummary(
                group.Key,
                group.Count()))
            .OrderBy(summary => summary.Year)
            .ToList();
    }

    public static IReadOnlyList<YearGroupSummary> ByYearQuerySyntax(
        IEnumerable<BookCopy> copies)
    {
        ArgumentNullException.ThrowIfNull(copies);

        return (
            from copy in copies
            group copy by copy.Year into yearGroup
            orderby yearGroup.Key
            select new YearGroupSummary(
                yearGroup.Key,
                yearGroup.Count()))
            .ToList();
    }

    public static bool YearGroupsMatch(
        IReadOnlyList<YearGroupSummary> methodSyntax,
        IReadOnlyList<YearGroupSummary> querySyntax)
    {
        ArgumentNullException.ThrowIfNull(methodSyntax);
        ArgumentNullException.ThrowIfNull(querySyntax);

        return methodSyntax.SequenceEqual(querySyntax);
    }

    public static CopyStatistics CopyTotals(
        IEnumerable<BookCopy> copies)
    {
        ArgumentNullException.ThrowIfNull(copies);

        List<int> copiesPerIsbn = copies
            .GroupBy(copy => copy.Isbn, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Count())
            .ToList();

        if (copiesPerIsbn.Count == 0)
        {
            return new CopyStatistics(0, 0, 0, 0);
        }

        return new CopyStatistics(
            copiesPerIsbn.Sum(),
            copiesPerIsbn.Count,
            copiesPerIsbn.Average(),
            copiesPerIsbn.Max());
    }
}
