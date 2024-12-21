using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WarframeMarketPriceMonitor;

internal static class ConfigHandler
{
    internal static readonly string ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), nameof(WarframeMarketPriceMonitor), "config.json");
    internal static JObject Config { get; set; }

    internal static void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));

        if (!File.Exists(ConfigPath))
        {
            Config = new JObject();

            // TODO

            var json = Config.ToString(Formatting.None);
            File.WriteAllText(ConfigPath, json);
        }
        else
        {
            Config = JObject.Parse(File.ReadAllText(ConfigPath));
        }
    }

    internal static Dictionary<string, int> GetMonitors()
    {
        var ret = new Dictionary<string, int>();
        if (Config.TryGetValue("PriceMonitors", out var monitors))
        {
            foreach (var monitor in monitors)
            {
                var key = monitor["ItemId"].ToString();
                var value = monitor["MaxPrice"].ToObject<int>();
                ret.Add(key, value);
            }
        }
        else
        {
            Config["PriceMonitors"] = new JArray();
        }
        return ret;
    }
    internal static void SetMonitors(Dictionary<string, int> monitors)
    {
        var jArray = new JArray();
        foreach (var (key, value) in monitors)
        {
            var jObject = new JObject
            {
                { "ItemId", key },
                { "MaxPrice", value }
            };
            jArray.Add(jObject);
        }
        Config["PriceMonitors"] = jArray;
    }

    internal static void Save()
    {
        var json = Config.ToString(Formatting.None);
        File.WriteAllText(ConfigPath, json);
    }
}
