using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace XmlShit;

internal partial class Program
{
    private static async Task Main(string[] args)
    {
        CorrectIndextermInNumberingtext(args.Where(f => File.Exists(f) && Path.GetExtension(f).Equals(".xml", StringComparison.OrdinalIgnoreCase)).ToArray());
    }

    private static readonly XmlReaderSettings _readerSettings = new XmlReaderSettings()
    {
        DtdProcessing = DtdProcessing.Parse,
        XmlResolver = new XmlUrlResolver()
    };
    private static readonly XmlWriterSettings _writerSettings = new XmlWriterSettings()
    {
        Encoding = Encoding.UTF8,
        Indent = false
    };

    private static void CorrectIndextermInNumberingtext(string[] files)
    {
        Console.WriteLine("Verarbeite XMLs:");
        for (var i = 0; i < files.Length; i++)
        {
            var path = files[i];
            Console.WriteLine($"    '{path}'");

            var reader = XmlReader.Create(path, _readerSettings);
            var doc = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            reader.Dispose();
            var ns = doc.Root.Name.Namespace;

            var wrongItems = doc.Descendants("item").Where(i => i.Attribute("numberingtext")?.Value?.Contains("XE \"") is true).ToArray();
            foreach (var item in wrongItems)
            {
                var attrib = item.Attribute("numberingtext");
                var numberingText = attrib.Value;
                var xeMatches = XEContentRegex().Matches(numberingText).ToArray();
                Array.Reverse(xeMatches);
                foreach (var match in xeMatches)
                {
                    var content = match.Groups["cnt"].Value;
                    var parts = content.Split(':');
                    var indexterm = new XElement(ns + "indexterm");
                    if (parts.Length > 0)
                    {
                        indexterm.Add(new XElement("primary", parts[0]));
                        attrib.Value = attrib.Value.Replace($"XE \"{content}\"", "").Trim();
                    }
                    if (parts.Length > 1)
                    {
                        indexterm.Add(new XElement("secondary", parts[1]));
                        attrib.Value = attrib.Value.Replace($"XE \"{content}\"", "").Trim();
                    }
                    if (parts.Length > 2)
                    {
                        indexterm.Add(new XElement("tertiary", parts[2]));
                        attrib.Value = attrib.Value.Replace($"XE \"{content}\"", "").Trim();
                    }
                    item.AddFirst(indexterm);
                }
            }

            using var writer = XmlWriter.Create(path, _writerSettings);
            doc.Save(writer);
        }
    }

    [GeneratedRegex("""
        XE\s"(?<cnt>.*?)"
        """, RegexOptions.ExplicitCapture)]
    private static partial Regex XEContentRegex();
}