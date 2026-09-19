namespace ACMEMarketData.Infrastructure.Components.Venue;

public sealed class SimulationSettings
{
    public int NumberOfParticipants { get; set; } = 100;
    public double Rate { get; set; } = 0.1;
    public (long Lower, long Upper) OrderSizeRange { get; set; } = (100, 1_000_000);
}