namespace ACMEMarketData.Infrastructure.Components.Participant;

public class Participant
{
    public Guid Id { get; } = Guid.NewGuid();

    public required string Name { get; init; }

    public static Participant Random(string? name = null)
    {
        return new Participant
        {
            Name = string.IsNullOrEmpty(name)
                ? Guid.NewGuid().ToString("N")
                : name
        };
    }

    public Order GenerateOrder(
        string venueCode,
        string instrument,
        ParticipantActionType actionType,
        decimal price,
        long quantity)
    {
        return new Order
        {
            ActionType = actionType,
            ParticipantId = Id,
            VenueCode = venueCode,
            Instrument = instrument,
            Price = price,
            OriginalQuantity = quantity,
            RemainingQuantity = quantity,
            State = OrderState.New
        };
    }
}