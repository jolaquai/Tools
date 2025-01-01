namespace Monitors.Shared.Services;

public static class Extensions
{
    public static bool Majority<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (!source.TryGetNonEnumeratedCount(out var total))
        {
            total = source.Count();
        }
        var count = source.Count(predicate);
        return count > total / 2;
    }
}
