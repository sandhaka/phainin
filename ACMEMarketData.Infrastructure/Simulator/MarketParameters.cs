namespace ACMEMarketData.Infrastructure.Simulator;

public sealed class MarketParameters
{
    public decimal PriceTickSize { get; set; }
    public List<string> OrderTypes { get; set; } = [];
}