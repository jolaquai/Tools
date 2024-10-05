using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using DbdOverlay.Model;
using DbdOverlay.Utility.Extensions;

using HtmlAgilityPack;

using Microsoft.Extensions.DependencyInjection;

namespace DbdOverlay.Utility;

public static partial class IServiceCollectionExtensions
{
    public static async Task AddPerks(this IServiceCollection svcs)
    {
        using var client = new HttpClient();
        const string perksUrl = @"https://deadbydaylight.fandom.com/wiki/Perks";

        // This entire thing, including traversing/parsing, only takes like 400ms... insane shit xd
        var page = await client.GetStringAsync(perksUrl);
        var doc = new HtmlDocument();
        doc.LoadHtml(page);

        var survivorTable = doc.DocumentNode.Descendants("h3")
            .First(static h3 => h3.InnerText.Contains("Survivor Perks", StringComparison.OrdinalIgnoreCase))
            .SiblingsAfter()
            .First(static e => e.Name == "table");
        var killerTable = doc.DocumentNode.Descendants("h3")
            .First(static h3 => h3.InnerText.Contains("Killer Perks", StringComparison.OrdinalIgnoreCase))
            .SiblingsAfter()
            .First(static e => e.Name == "table");

        var cb = new ConcurrentBag<Perk>();
        await Task.WhenAll(
            Parallel.ForEachAsync(survivorTable.Descendants("tr").Skip(1), async (row, _) =>
            {
                var cells = row.ChildNodes.Where(static e => e.Name is "th" or "td").ToArray();
                if (cells.Length == 0) return;

                var title = cells[1].InnerText;
                var description = cells[2].InnerText;
                cb.Add(new Perk()
                {
                    Title = title,
                    Description = description,
                    For = "Survivor"
                });
            }),
            Parallel.ForEachAsync(killerTable.Descendants("tr").Skip(1), async (row, _) =>
            {
                var cells = row.ChildNodes.Where(static e => e.Name is "th" or "td").ToArray();
                if (cells.Length == 0) return;

                var title = cells[1].InnerText;
                var description = cells[2].InnerText;
                cb.Add(new Perk()
                {
                    Title = title,
                    Description = description,
                    For = "Killer"
                });
            })
        );
        foreach (var perk in cb)
        {
            svcs.AddSingleton(perk);
        }
    }
}
