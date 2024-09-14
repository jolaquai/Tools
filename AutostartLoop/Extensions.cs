namespace AutostartLoop;
public static class Extensions
{
    public static bool Majority<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var enumerated = source as T[] ?? source.ToArray();
        var count = source.Count(predicate);
        return count > enumerated.Length / 2;
    }
}
