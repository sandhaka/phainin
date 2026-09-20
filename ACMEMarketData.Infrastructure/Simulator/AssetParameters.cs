namespace ACMEMarketData.Infrastructure.Simulator;

public sealed class AssetParameters
{
    public string Symbol { get; set; } = string.Empty;
    public decimal InitialPrice { get; set; }
    public double PriceVolatility { get; set; }
}