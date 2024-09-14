using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using PdfConverter.Model;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace PdfConverter;

public static class Program
{
    private static readonly ContentMetaData _contentMetaData = new ContentMetaData()
    {
        LeftPage = new Rectangle(53, 84.4, 360.5, 558.3),
        RightPage = new Rectangle(67.5, 84.4, 374.5, 558.3),
        // TODO: According to this, combine paragraphs
        MaxParagraphLineHeight = 12,
        LeftDefaultLineStart = 53.857,
        RightDefaultLineStart = 68.032,
        MaxLineStartDifference = 0.5
    };

    public static async Task Main(string[] args)
    {
        var path = args.Single(a => Path.GetExtension(a).Equals(".pdf", StringComparison.OrdinalIgnoreCase));

        using var pdf = PdfDocument.Open(path);

        // To debug, get a specific page and Step Into ProcessPage(Page)
        if (false)
        {
            var fifth = pdf.GetPage(5);
            ProcessPage(fifth);
        }

        var paragraphs = pdf.GetPages()
            .SelectMany(pageModel => ProcessPage(pageModel))
            .ToArray();

        await using (var fs = File.OpenWrite(Path.ChangeExtension(path, ".docx")))
        await using (var ms = new MemoryStream())
        {
            using (var wpd = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                var mainPart = wpd.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());
                body.Append(paragraphs);
            }

            ms.Seek(0, SeekOrigin.Begin);
            await ms.CopyToAsync(fs);
        }
    }

    private static Paragraph[] ProcessPage(Page page)
    {
        var text = page.GetWords();
        var lines = text
            .GroupBy(w => w.BoundingBox.Bottom, EpsilonEqualityComparer.Instance)
            .OrderByDescending(g => g.Key)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Keep only lines that are within the page's content bounds
        // The key of each grouping is the bottom of the line
        // Also, inside lines, throw out all individual Word objects that are outside the area
        var area = page.Number % 2 == 0 ? _contentMetaData.RightPage : _contentMetaData.LeftPage;
        var linesWithinArea = lines
            // Omit KTs, author names, page numbers etc.
            .Where(kvp => kvp.Key <= area.Top && kvp.Key >= area.Bottom)
            .Select(kvp =>
            {
                // Omit all words that are outside the vertical bounds
                // kvp.Value.RemoveAll(w => w.BoundingBox.Left < area.Left || w.BoundingBox.Right > area.Right);
                return kvp;
            })
            // Now throw out lines that have no words left
            .Where(kvp => kvp.Value.Count > 0)
            .Select(kv => kv.Value.ToArray())
            .ToArray();

        // TODO: Find footnote separator line
        // TODO: Move all lines starting with a number to the end of the lines collection
        {

        }

        // TODO: Ensure correct line order for 'Übersicht' sections
        //       -> This will probably require a lot of heavy sorting involving the x-positions of the first word in each line, considering that the first word in each line is always indented and then there's shit going on with the second column, which usually just gets put into the same line
        // We're gonna do this by finding all non-standard-starting lines and sorting just those ranges of the line array

        // Now that we have lines, split them into paragraphs
        // The main issue is that we need to know the exact difference between one line to the next, and if and only if that difference between two lines is greater that that "line height", a new paragraph should be started
        var asParagraphs = linesWithinArea
            .Aggregate(new List<List<Word>>(),
            (acc, line) =>
            {
                if (acc.Count == 0)
                {
                    acc.Add([.. line]);
                    return acc;
                }

                var lastLine = acc[^1];
                var lastLineBottom = lastLine[^1].BoundingBox.Bottom;
                var currentLineTop = line[^1].BoundingBox.Top;

                if (Math.Abs(lastLineBottom - currentLineTop) > _contentMetaData.MaxParagraphLineHeight)
                {
                    acc.Add([.. line]);
                    return acc;
                }

                lastLine.AddRange(line);
                return acc;
            })
            .Select(words =>
            {
                // These are gonna be the "mock Runs" for the paragraph
                List<TextChunk> chunks = [];
                TextChunk current = null;

                var wordCount = words.Count;
                for (var wordIndex = 0; wordIndex < wordCount; wordIndex++)
                {
                    var word = words[wordIndex];

                    var letterCount = word.Letters.Count;
                    for (var letterIndex = 0; letterIndex < letterCount; letterIndex++)
                    {
                        var letter = word.Letters[letterIndex];

                        var bold = letter.Font.Name.EndsWith('b');
                        var italic = letter.Font.Name.EndsWith('i');
                        // Round to the nearest 0.5 since that's all Word supports... shitty program...
                        //var fontSize = Math.Round(letter.PointSize * 2) / 2;
                        var letterValue = letter.Value;

                        current ??= new TextChunk()
                        {
                            Bold = bold,
                            Italic = italic
                        };

                        if (current.Bold != bold || current.Italic != italic /*|| current.FontSize != fontSize*/)
                        {
                            chunks.Add(current);
                            current = new TextChunk()
                            {
                                Bold = bold,
                                Italic = italic
                            };
                        }
                        else if (letterIndex == letterCount - 1)
                        {
                            current.Append(letterValue);
                            current.Append(" ");
                            current.Freeze();
                            chunks.Add(current);
                            current = null;
                        }
                        else
                        {
                            current.Append(letterValue);
                        }
                    }
                }
                return new PdfParagraph(chunks);
            })
            .ToArray();

        var openXmlParas = asParagraphs
            .Select(para =>
            {
                var paragraph = new Paragraph();
                foreach (var toBeRun in para)
                {
                    var run = new Run();
                    if (toBeRun.Bold || toBeRun.Italic)
                    {
                        run.RunProperties = new RunProperties()
                        {
                            Bold = toBeRun.Bold ? new Bold() : null,
                            Italic = toBeRun.Italic ? new Italic() : null
                        };
                    }

                    var text = new Text(toBeRun.Text);
                    if (toBeRun.Text.StartsWith(' ') || toBeRun.Text.EndsWith(' '))
                    {
                        text.Space = SpaceProcessingModeValues.Preserve;
                    }

                    run.Append(text);
                    paragraph.Append(run);
                }

                // If the last run we appended doesn't end with a space, add one
                if (paragraph.LastChild is Run lastRun && lastRun.LastChild is Text lastText && !lastText.Text.EndsWith(' '))
                {
                    lastText.Text += ' ';
                }
                return paragraph;
            })
            .ToArray();

        //foreach (var para in openXmlParas)
        //{
        //    var emptyElements = para
        //        .Descendants()
        //        .Where(e => !e.HasChildren && string.IsNullOrEmpty(e.InnerText))
        //        .ToArray();
        //    foreach (var element in emptyElements)
        //    {
        //        element.Remove();
        //    }
        //}

        return openXmlParas;
    }
}
internal sealed class EpsilonEqualityComparer : IEqualityComparer<double>
{
    private readonly double _epsilon;

    private EpsilonEqualityComparer(double epsilon = 0.05) => _epsilon = epsilon;
    public static EpsilonEqualityComparer Instance { get; } = new EpsilonEqualityComparer();

    public bool Equals(double x, double y) => Math.Abs(x - y) < _epsilon;

    public int GetHashCode(double obj) => obj.GetHashCode();
}
