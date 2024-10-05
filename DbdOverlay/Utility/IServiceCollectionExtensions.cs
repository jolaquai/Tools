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
            Parallel.ForEachAsync(survivorTable.Descendants("tr"), async (row, _) =>
            {
                var cells = row.ChildNodes.Where(static e => e.Name is "th" or "td").ToArray();
                if (cells.Length == 0) return;

                var title = cells[1].InnerText;
                var description = cells[2].InnerText;
                cb.Add(new Perk()
                {
                    Title = title,
                    Description = description,
                    Icon = await CreateIconImageSourceAsync(client, cells[0]),
                    For = "Survivor"
                });
            }),
            Parallel.ForEachAsync(killerTable.Descendants("tr"), async (row, _) =>
            {
                var cells = row.ChildNodes.Where(static e => e.Name is "th" or "td").ToArray();
                if (cells.Length == 0) return;

                var title = cells[1].InnerText;
                var description = cells[2].InnerText;
                cb.Add(new Perk()
                {
                    Title = title,
                    Description = description,
                    Icon = await CreateIconImageSourceAsync(client, cells[0]),
                    For = "Killer"
                });
            })
        );
        foreach (var perk in cb)
        {
            svcs.AddSingleton(perk);
        }
    }

    private static readonly Regex _perkIconNameRegex = PerkIconNameRegex();
    private static async Task<ImageSource> CreateIconImageSourceAsync(HttpClient client, HtmlNode iconCell)
    {
        var href = iconCell.Descendants("a").First().GetAttributeValue("href", "");
        var fileName = _perkIconNameRegex.Match(href).GetSubmatch("fileName");
        if (fileName is null) return null;

        const string baseUrl = @"https://static.wikia.nocookie.net/deadbydaylight_gamepedia_en/images/2/24/";
        var uri = new Uri(baseUrl + fileName);
        var pngData = await client.GetByteArrayAsync(uri);
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.StreamSource = new MemoryStream(pngData);
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    [GeneratedRegex(@"(?<=/)(?<fileName>(?>IconPerks_.*\.png))")]
    private static partial Regex PerkIconNameRegex();
}
