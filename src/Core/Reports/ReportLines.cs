namespace Core.Reports;

public sealed record TopReaderLine(
    string ReaderId,
    int LoanCount);

public sealed record OverdueLoanLine(
    string LoanId,
    string BookCopyId,
    string ReaderId,
    DateTime IssuedOn,
    DateTime DueOn,
    int DaysOverdue);

public sealed record YearGroupSummary(
    int Year,
    int CopyCount);

public sealed record CopyStatistics(
    int TotalCopies,
    int DistinctEditions,
    double AverageCopiesPerEdition,
    int MaxCopiesOfOneEdition);

public sealed record CopyLoanSummary(
    string CopyId,
    string Isbn,
    int LoanCount,
    int OpenLoanCount);

public sealed record TopCopyLine(
    string CopyId,
    string Isbn,
    int LoanCount);

public sealed record BenchmarkReport(
    int InputCount,
    int ResultCount,
    bool ResultsMatch,
    IReadOnlyList<long> VariantAMilliseconds,
    IReadOnlyList<long> VariantBMilliseconds,
    long VariantAMedianMilliseconds,
    long VariantBMedianMilliseconds);
