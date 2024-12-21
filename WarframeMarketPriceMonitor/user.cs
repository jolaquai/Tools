namespace WarframeMarketPriceMonitor;
internal record class user
{
    public int reputation { get; init; }
    public string locale { get; init; }
    public string avatar { get; init; }
    public DateTime last_seen { get; init; }
    public string ingame_name { get; init; }
    public string id { get; init; }
    public string region { get; init; }
    public string status { get; init; }
}
