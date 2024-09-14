using System.CodeDom.Compiler;

using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocxBreaker;

internal class Program
{
    static void Main(string[] args)
    {
        var files = args.Where(a => Path.GetExtension(a).ToUpperInvariant() is ".DOCM" or ".DOCX").Where(File.Exists);
        var dirs = args.Where(Directory.Exists);
        var all = files.Concat(dirs.SelectMany(d => Directory.EnumerateFiles(d, "*.*", SearchOption.AllDirectories))).ToArray();

        using var console = new IndentedTextWriter(Console.Out, "    ");

        Array.ForEach(all, path =>
        {
            console.WriteLine($"Verarbeite '{path}'...");
            console.Indent++;

            using var fs = File.OpenRead(path);
            using var wpd = WordprocessingDocument.Open(fs, false);

            using var newDocStream = File.Open(Path.ChangeExtension(path, ".broken.docx"), FileMode.Create);
            using var newDoc = WordprocessingDocument.Create(newDocStream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);

            var originalBody = wpd?.MainDocumentPart?.Document?.Body;
            if (originalBody is null)
            {
                console.WriteLine("Originaldokument hat keinen Inhalt.");
            }

            var mdp = newDoc.AddMainDocumentPart();
            var document = mdp.Document ??= new Document();
            var newBody = document.AppendChild(new Body());

            var paragraphs = originalBody?.Descendants<Paragraph>().ToArray();
            if (paragraphs is null)
            {
                console.WriteLine("Originaldokument hat keinen Inhalt.");
            }

            newBody.Append(paragraphs
                .Where(p => !string.IsNullOrWhiteSpace(p.InnerText))
                .Select(p => new Paragraph(new Run(new Text(p.InnerText))))
                .ToArray()
            );

            console.WriteLine("Fertig.");
            console.Indent--;
        });

        console.WriteLine();
        console.WriteLine("Zum Beenden Strg+C drücken.");
        while (true)
        {
            Console.ReadLine();
        }
    }
}
