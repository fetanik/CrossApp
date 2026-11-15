using Core.Domain;

namespace Core.Reports;

public sealed record DeferredExecutionReport(
    int CountBeforeAdd,
    int CountAfterAdd,
    int SnapshotCount,
    int CountAfterClear,
    int SnapshotCountAfterClear);

public static class DeferredExecutionDemo
{
    public static DeferredExecutionReport Run()
    {
        var copies = new List<BookCopy>
        {
            BookCopy.Create(
                "D-001",
                "9780000000001",
                "Книга А",
                1990,
                "Автор А"),
            BookCopy.Create(
                "D-002",
                "9780000000002",
                "Книга Б",
                2010,
                "Автор Б")
        };

        IEnumerable<BookCopy> query =
            copies.Where(copy => copy.Year >= 2000);

        int countBeforeAdd = query.Count();

        copies.Add(BookCopy.Create(
            "D-003",
            "9780000000003",
            "Книга В",
            2020,
            "Автор В"));

        int countAfterAdd = query.Count();
        List<BookCopy> snapshot = query.ToList();
        int snapshotCount = snapshot.Count;

        copies.Clear();

        return new DeferredExecutionReport(
            countBeforeAdd,
            countAfterAdd,
            snapshotCount,
            query.Count(),
            snapshot.Count);
    }
}
