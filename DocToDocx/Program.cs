using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2010.Word.DrawingShape;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordFileShit;

internal class Program
{
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var ex = (Exception)e.ExceptionObject;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"'{e.ExceptionObject.GetType().FullName}': '{ex.Message}' ({ex.HResult & 0xFFFF})");
            Console.WriteLine(ex.StackTrace);
        };

        foreach (var path in Directory.GetFiles(args[0]))
        {
            using var fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var wpd = WordprocessingDocument.Open(fs, true);

            var parasInTextbox = wpd.MainDocumentPart.Document.Body.Descendants<Paragraph>().Where(p => p.Ancestors<TextBoxContent>().Any() && !p.Ancestors<AlternateContentFallback>().Any()).ToArray();
            foreach (var para in parasInTextbox)
            {
                var textbox = para.Ancestors<TextBoxContent>().First();
                var run = textbox.Ancestors<Run>().First();
                var parentPara = run.Ancestors<Paragraph>().First();

                var clone = (Paragraph)para.CloneNode(true);
                if (clone.ParagraphProperties is ParagraphProperties pPr)
                    pPr.Justification?.Remove();

                parentPara.InsertBeforeSelf(clone);
                para.Remove();
            }

            var tbxContents = wpd.MainDocumentPart.Document.Body.Descendants<TextBoxContent>().ToArray();
            foreach (var content in tbxContents)
            {
                if (content.ChildElements.Count == 0)
                {
                    var anc = content.Ancestors<TextBoxInfo2>().ToArray();
                    if (anc.Length > 0)
                    {
                        var textbox = anc[0];
                        var shape = textbox.Ancestors<WordprocessingShape>().First();
                        var pict = shape.Ancestors<Drawing>().First();
                        pict.Remove();
                    }
                    else
                    {
                        var textbox = content.Ancestors<TextBox>().First();
                        var shape = textbox.Ancestors<Shape>().First();
                        var pict = shape.Ancestors<Picture>().First();
                        pict.Remove();
                    }
                }
            }

            var alternateContents = wpd.MainDocumentPart.Document.Body.Descendants<AlternateContent>().ToArray();
            foreach (var content in alternateContents)
            {
                var run = content.Ancestors<Run>().First();
                var children = run.Elements().Where(e => e is not RunProperties).ToArray();
                if (content.ChildElements is [AlternateContentChoice, AlternateContentFallback] && children.Length == 1)
                {
                    run.Remove();
                }
            }
        }
    }
}
