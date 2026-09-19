namespace ACMEMarketData.Infrastructure.Components.Participant;

internal class Participant
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;

    public Order GenerateOrder(string instrument, ParticipantActionType actionType, decimal price, long quantity)
    {
        return new Order
        {
            ActionType = actionType,
            ParticipantId = Id,
            Instrument = instrument,
            Price = price,
            OriginalQuantity = quantity,
            RemainingQuantity = quantity,
            State = OrderState.New
        };
    }
}