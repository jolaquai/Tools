using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace XmlDtdDecoupler;

internal partial class Program
{
    private static XDocument Cache;

    static void Main(string[] args)
    {
        var options = Array.FindAll(args, a => a.StartsWith("--"));
        args = Array.FindAll(args, a => a.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

        if (args.Length == 0)
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No XML files found to process.");
                Console.ResetColor();
            }
            return;
        }

        var online = Array.Exists(options, o => o.Equals("--online", StringComparison.OrdinalIgnoreCase));

        Cache = GetCache();

        foreach (var file in args)
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Processing '{file}'...");
                Console.ResetColor();
            }

            var xml = File.ReadAllText(file);
            var numericEntity = NumericEntityRegex().Match(xml);
            var namedEntities = NamedEntityRegex().Match(xml);

            var numericResolutions = 0;
            var namedFromCache = 0;
            var newNameds = 0;

            HashSet<int> uniqueNumeric = [];
            if (options.Contains("--include-numeric"))
            {
                while (numericEntity.Success)
                {
                    int definition;
                    if (numericEntity.Groups["hex"].Success)
                    {
                        definition = Convert.ToInt32(numericEntity.Groups["hex"].Value, fromBase: 16);
                    }
                    else
                    {
                        definition = int.Parse(numericEntity.Groups["dec"].Value);
                    }

                    uniqueNumeric.Add(definition);

                    if (Cache.Root.Elements().FirstOrDefault(e => e.Name == "entity"
                            && e.Element("definition")?.Value is string value
                            && value == definition.ToString()) is XElement entity
                    )
                    {
                        var value = entity.Element("value")?.Value;
                        xml = xml.Replace(numericEntity.Value, value);
                    }
                    else
                    {
                        var value = ((char)definition).ToString();
                        RegisterNumericEntity(definition);
                        xml = xml.Replace(numericEntity.Value, value);
                    }

                    numericResolutions++;

                    numericEntity = numericEntity.NextMatch();
                }
            }

            HashSet<string> uniqueNamedCached = [];
            var hasInformed = false;
            while (namedEntities.Success)
            {
                var definition = namedEntities.Groups["name"].Value;

                if (Cache.Root.Elements().FirstOrDefault(e => e.Name == "entity"
                        && e.Element("definition")?.Value is string value
                        && value == definition) is XElement entity
                )
                {
                    namedFromCache++;
                    uniqueNamedCached.Add(definition);

                    var value = entity.Element("value")?.Value;
                    xml = xml.Replace(namedEntities.Value, value);
                }
                else
                {
                    string value;
                    lock (Console.Out)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"    Named entity '{definition}' not found in cache. Please supply the string to substitute it with.");
                        if (!hasInformed)
                        {
                            Console.WriteLine("    Notes: - If you input a pure signed 32-bit integer, it is assumed to be a Unicode code point. It is evaluated exactly as '&#[INPUT];'.");
                            Console.WriteLine("           - If you input a hex number (denoted by prepending your input with a lowercase 'x'), it is assumed to be a Unicode code point. It is evaluated exactly as '&#x[INPUT];'.");
                            Console.WriteLine("           - You may override this behavior by prepending your input with a quote '\"', which will cause your input to be treated literally. Any further quotes are included in that literal.");
                            hasInformed = true;
                        }
                        Console.Write("    >> ");
                        value = Console.ReadLine()!;
                        if (value.StartsWith('"'))
                        {
                            value = value[1..];
                        }
                        else if (int.TryParse(value, out var parsed))
                        {
                            value = ((char)parsed).ToString();
                        }
                        else if (value.StartsWith('x'))
                        {
                            value = ((char)Convert.ToInt32(value[1..], fromBase: 16)).ToString();
                        }
                        Console.ResetColor();
                    }

                    newNameds++;

                    RegisterNamedEntity(definition, value);
                    xml = xml.Replace(namedEntities.Value, value);
                }

                namedEntities = namedEntities.NextMatch();
            }

            File.WriteAllText(file, xml);
            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    Finished processing '{file}'.");
                Console.WriteLine($"      Processed {numericResolutions + newNameds + namedFromCache} entities total.");
                Console.WriteLine($"        Resolved {numericResolutions} numeric entities.");
                Console.WriteLine($"          Of those, {uniqueNumeric.Count} were unique.");
                Console.WriteLine($"        Persisted {newNameds} new named entities to cache.");
                Console.WriteLine($"        Resolved {namedFromCache} named entities from cache.");
                Console.WriteLine($"          Of those, {uniqueNamedCached.Count} were unique.");
                Console.ResetColor();
            }
        }

        Directory.CreateDirectory(@".\Resources");
        Cache.Save(@".\Resources\config.xml");
    }

    #region Entity
    private static void RegisterNumericEntity(int definition)
    {
        Cache.Root.Add(new Entity(definition.ToString(), ((char)definition).ToString(), "numeric").ToXml());
    }
    private static void RegisterNamedEntity(string definition, string value)
    {
        Cache.Root.Add(new Entity(definition, value, "named").ToXml());
    }
    private struct Entity(string definition, string value, string mode)
    {
        public string Definition { get; set; } = definition;
        public string Value { get; set; } = value;
        public string Mode { get; set; } = mode;
        public readonly XElement ToXml()
        {
            return new XElement("entity", new XAttribute("mode", Mode),
                new XElement("definition", Definition),
                new XElement("value", Value)
            );
        }
        public static implicit operator XElement(Entity entity) => entity.ToXml();
        public static Entity FromXml(XElement xelement)
        {
            return new Entity(
                xelement.Element("definition")?.Value,
                xelement.Element("value")?.Value,
                xelement.Attribute("mode")?.Value
            );
        }
        public static implicit operator Entity(XElement xelement) => FromXml(xelement);
    }
    #endregion

    [GeneratedRegex(@"&#x(?<hex>[0-9a-fA-F]{1,5});|&#(?<dec>[0-9]+?);", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex NumericEntityRegex();
    [GeneratedRegex(@"&(?<name>\S+?);", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex NamedEntityRegex();

    private static XDocument GetCache()
    {
        if (!File.Exists(@".\Resources\config.xml"))
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(@".\Resources\config.xml not found. Building new cache.");
                Console.ResetColor();
            }
            return new XDocument(new XElement("entities"));
        }

        return XDocument.Load(@".\Resources\config.xml");
    }
}
