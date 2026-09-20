namespace ACMEMarketData.Infrastructure.Simulator;

public sealed class SimulationParameters
{
    public int NumberOfParticipants { get; set; } = 1;
    public double OrdersPerSecond { get; set; } = 1;
    public OrderSizeRangeParameters OrderSizeRange { get; set; } = new();
}