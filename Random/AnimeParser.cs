using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using CsvHelper;
using CsvHelper.Configuration;

namespace _Random;

internal static partial class AnimeParser
{
    public static async Task ParseAnimesAsync()
    {
        using var fs = File.OpenRead(@"E:\PROGRAMMING\Projects\html\baycode\anime-dataset-2023.csv");
        using var sr = new StreamReader(fs);
        using var parser = new CsvParser(sr, new CsvConfiguration(new CultureInfo("en-us")));

        using (var outFile = File.Create(@"E:\PROGRAMMING\Projects\html\baycode\test.jj"))
        using (var sw = new StreamWriter(outFile))
        {
            List<Anime> list = [];
            while (parser.Read())
            {
                if (parser.Row == 1)
                {
                    continue;
                }

                list.Add(new Anime()
                {
                    Id = float.TryParse(parser[0], out var id) ? (int)id : null,
                    //Name = parser[1],
                    EnglishName = parser[2],
                    //OtherName = parser[3],
                    //Score = float.TryParse(parser[4], out var score) ? score : null,
                    Genres = parser[5].Split(", "),
                    Synopsis = parser[6],
                    Type = parser[7],
                    //Episodes = float.TryParse(parser[8], out var epCount) ? (int)epCount : null,
                    Aired = parser[9],
                    //Premiered = parser[10],
                    //Status = parser[11],
                    //Producers = parser[12].Split(", "),
                    //Licensors = parser[13].Split(", "),
                    //Studios = parser[14].Split(", "),
                    //Source = parser[15],
                    //Duration = parser[16],
                    //Rating = parser[17],
                    Rank = float.TryParse(parser[18], out var rank) ? rank : null,
                    //Popularity = float.TryParse(parser[19], out var popularity) ? (int)popularity : null,
                    //Favorites = parser[20],
                    //ScoredBy = float.TryParse(parser[21], out var scoredBy) ? scoredBy : null,
                    //Members = parser[22],
                    ImageUrl = parser[23],
                });
            }

            var fields = typeof(Anime).GetFields(BindingFlags.Public | BindingFlags.Instance);
            fields = Array.FindAll(fields, f => f.GetValue(list[0]) is not null);

            list.RemoveAll(a => a.Type?.Equals("music", StringComparison.OrdinalIgnoreCase) == true);
            list.RemoveAll(a => a.EnglishName?.Equals("unknown", StringComparison.OrdinalIgnoreCase) == true
                || (a.Genres?.Length is > 0 && a.Genres[0].Equals("unknown", StringComparison.OrdinalIgnoreCase) == true) == true
                || string.IsNullOrWhiteSpace(a.Synopsis) == true
                || a.Synopsis?.Equals("unknown", StringComparison.OrdinalIgnoreCase) == true);

            sw.Write('[');
            var count = list.Count;
            for (var i = 0; i < count; i++)
            {
                var anime = list[i];
                var obj = fields.ToFrozenDictionary(f => f.Name, f =>
                {
                    var val = f.GetValue(anime);
                    var parsed = val switch
                    {
                        null => "",
                        _ => val,
                    };
                    return parsed;
                });
                sw.Write($"{{{string.Join(',', obj.Select(kv => kv.Value switch
                {
                    string s => $"{kv.Key}:`{SourceTrimRegex().Replace(s.Replace('`', '\''), "")}`",
                    IEnumerable<string> => $"{kv.Key}:[{string.Join(", ", ((IEnumerable<string>)kv.Value).Select(s => $"`{s}`"))}]",
                    int or float => $"{kv.Key}:{kv.Value}",
                }))}}}");

                if (i != count - 1)
                {
                    sw.Write(',');
                }
            }
            sw.Write(']');
        }
    }

    [GeneratedRegex(@"[\r\n]{2,}\(Source: .*?\)$")]
    private static partial Regex SourceTrimRegex();

    private struct Anime
    {
        public int? Id;
        public string? Name;
        public string? EnglishName;
        public string? OtherName;
        public float? Score;
        public string[]? Genres;
        public string? Synopsis;
        public string? Type;
        public int? Episodes;
        public string? Aired;
        public string? Premiered;
        public string? Status;
        public string[]? Producers;
        public string[]? Licensors;
        public string[]? Studios;
        public string? Source;
        public string? Duration;
        public string? Rating;
        public float? Rank;
        public int? Popularity;
        public string? Favorites;
        public float? ScoredBy;
        public string? Members;
        public string? ImageUrl;
    }
}
