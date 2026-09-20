namespace ACMEMarketData.Infrastructure.Simulator;

public sealed class OrderSizeRangeParameters
{
    public long Lower { get; set; } = 100;
    public long Upper { get; set; } = 1_000_000;
}