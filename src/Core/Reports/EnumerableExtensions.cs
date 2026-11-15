namespace Core.Reports;

public static class EnumerableExtensions
{
    public static IReadOnlyList<T> TopBy<T, TTie>(
        this IEnumerable<T> source,
        Func<T, int> score,
        int count,
        Func<T, TTie> tieBreak,
        IComparer<TTie>? tieBreakComparer = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(score);
        ArgumentNullException.ThrowIfNull(tieBreak);

        if (count <= 0)
        {
            return [];
        }

        return source
            .OrderByDescending(score)
            .ThenBy(tieBreak, tieBreakComparer ?? Comparer<TTie>.Default)
            .Take(count)
            .ToList();
    }
}
