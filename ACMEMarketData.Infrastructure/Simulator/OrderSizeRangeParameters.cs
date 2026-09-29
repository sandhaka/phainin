namespace ACMEMarketData.Infrastructure.Simulator;

public sealed class OrderSizeRangeParameters
{
    public int Lower { get; set; } = 100;
    public int Upper { get; set; } = 1_000_000;
}