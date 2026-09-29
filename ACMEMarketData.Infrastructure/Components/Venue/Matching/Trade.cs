namespace ACMEMarketData.Infrastructure.Components.Venue.Matching;

public struct Trade
{
    public string Asset { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }

    public override string ToString() => $"Trade {Asset}@{Quantity}, Price: {Price:N2}";
}