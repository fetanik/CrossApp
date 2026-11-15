using System.Diagnostics;

namespace Core.Reports;

public readonly record struct ReadingSample(
    int Id,
    int Year,
    int DaysOnLoan);

public readonly record struct ReadingSampleLine(
    int Id,
    int DaysOnLoan);

public static class LinqBenchmark
{
    public const int RecordCount = 100_000;
    private const int MeasuredRuns = 5;

    public static BenchmarkReport Run()
    {
        List<ReadingSample> data = CreateData();

        List<ReadingSampleLine> RunVariantA() =>
            data.Where(item => item.Year >= 1950)
                .ToList()
                .OrderByDescending(item => item.DaysOnLoan)
                .Select(item => new ReadingSampleLine(
                    item.Id,
                    item.DaysOnLoan))
                .ToList();

        List<ReadingSampleLine> RunVariantB() =>
            data.Where(item => item.Year >= 1950)
                .OrderByDescending(item => item.DaysOnLoan)
                .Select(item => new ReadingSampleLine(
                    item.Id,
                    item.DaysOnLoan))
                .ToList();

        _ = RunVariantA();
        _ = RunVariantB();

        List<ReadingSampleLine> variantA = RunVariantA();
        List<ReadingSampleLine> variantB = RunVariantB();
        IReadOnlyList<long> timesA = Measure(RunVariantA);
        IReadOnlyList<long> timesB = Measure(RunVariantB);

        return new BenchmarkReport(
            data.Count,
            variantA.Count,
            variantA.SequenceEqual(variantB),
            timesA,
            timesB,
            Median(timesA),
            Median(timesB));
    }

    private static List<ReadingSample> CreateData()
    {
        var data = new List<ReadingSample>(RecordCount);

        for (int id = 0; id < RecordCount; id++)
        {
            data.Add(new ReadingSample(
                id,
                1800 + (id % 250),
                id % 40));
        }

        return data;
    }

    private static IReadOnlyList<long> Measure(
        Func<List<ReadingSampleLine>> query)
    {
        var times = new List<long>(MeasuredRuns);

        for (int run = 0; run < MeasuredRuns; run++)
        {
            var watch = Stopwatch.StartNew();
            _ = query();
            watch.Stop();
            times.Add(watch.ElapsedMilliseconds);
        }

        return times;
    }

    private static long Median(IReadOnlyList<long> times)
    {
        List<long> sorted = times
            .OrderBy(time => time)
            .ToList();

        return sorted[sorted.Count / 2];
    }
}
