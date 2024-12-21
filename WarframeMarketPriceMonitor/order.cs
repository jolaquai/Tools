namespace WarframeMarketPriceMonitor;
internal record class order
{
    public string order_type { get; init; }
    public int quantity { get; init; }
    public bool visible { get; init; }
    public int platinum { get; init; }
    public user user { get; init; }
    public string platform { get; init; }
    public DateTime creation_date { get; init; }
    public DateTime last_update { get; init; }
    public string id { get; init; }
    public string region { get; init; }
}
