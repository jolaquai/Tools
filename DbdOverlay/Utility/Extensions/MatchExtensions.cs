using System.Text.RegularExpressions;

namespace DbdOverlay.Utility.Extensions;
public static class MatchExtensions
{
    public static string? GetSubmatch(this Match match, string groupName)
    {
        if (match.Groups.TryGetValue(groupName, out var val))
            return val.Value;
        return null;
    }
}
