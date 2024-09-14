using System.IO.Compression;

namespace SplitPdf;

internal static class Program
{
    private static readonly HttpClient Client = new HttpClient()
    {
        BaseAddress = new Uri("https://flowdocx.becksche.de/PdfManipulatorApi/api/"),
        DefaultRequestHeaders =
        {
            { "api-version", "1.0" },
            { "api-token", "7FHgN6zAEYKtKJIRicf39YSo0D1MXBbVZpEqQ2MOLVN54hrLd8HGU" },
        },
        Timeout = TimeSpan.FromMinutes(10),
    };

    static async Task Main(string[] args)
    {
        var consoleLock = new object();

        if (args.Length == 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Keine Eingabedateien.");
            Console.ResetColor();
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Zu bearbeitende Dateien:");
        foreach (var arg in args)
        {
            Console.WriteLine($"    '{arg}'");
        }
        Console.WriteLine();

        var pagesPerFile = -1;
        Console.WriteLine("Seiten pro Datei?");
        Console.Write(">> ");
        while (!int.TryParse(Console.ReadLine(), out pagesPerFile))
        {
            Console.WriteLine("Bitte eine Zahl > 0 eingeben!");
            Console.Write(">> ");
        }

        Console.WriteLine();
        await Task.WhenAll(args.Select(async inputPdfPath =>
        {
            try
            {
                await Console.Out.WriteLineAsync($"Sende PDF an API: '{inputPdfPath}'");
                var mfd = new MultipartFormDataContent()
                {
                    { new ByteArrayContent(await File.ReadAllBytesAsync(inputPdfPath)), "FileToSplit", Path.GetFileName(inputPdfPath) },
                    { new StringContent(pagesPerFile.ToString()), "SplitPageCount" },
                };

                var response = await Client.PostAsync("Split", mfd);
                response.EnsureSuccessStatusCode();

                await Console.Out.WriteLineAsync($"Lese Rückgabe von API: '{inputPdfPath}'");
                using (var zip = new ZipArchive(await response.Content.ReadAsStreamAsync(), ZipArchiveMode.Read))
                {
                    Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(inputPdfPath), Path.GetFileNameWithoutExtension(inputPdfPath)));
                    await Console.Out.WriteLineAsync($"Schreibe Ausgabedateien: '{inputPdfPath}'");

                    foreach (var entry in zip.Entries.OrderBy(entry => entry.Name))
                    {
                        var outPath = @$"{Path.Combine(Path.GetDirectoryName(inputPdfPath), Path.GetFileNameWithoutExtension(inputPdfPath))}\{Path.GetFileNameWithoutExtension(inputPdfPath)}_{Path.GetFileNameWithoutExtension(entry.Name)}.pdf";
                        await using (var outFile = File.Create(outPath))
                        await using (var entryStream = entry.Open())
                        {
                            await entryStream.CopyToAsync(outFile);
                        }
                    }
                    lock (consoleLock)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"PDF bearbeitet: '{inputPdfPath}'");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Fehler bei PDF: '{inputPdfPath}'");
                    Console.WriteLine($"    {ex.GetType().FullName}: '{ex.Message}'");
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine();
                    Console.ResetColor();
                }
            }
        }));

        lock (consoleLock)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Alle Dateien bearbeitet.");
            Console.ResetColor();
        }

        Console.ReadLine();
    }
}
