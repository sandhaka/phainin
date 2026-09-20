namespace ACMEMarketData.Infrastructure.Components.Participant;

public sealed class Order
{
    public Guid OrderId { get; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
    
    public ParticipantActionType ActionType { get; init; }
    public Guid ParticipantId { get; init; }
    public required string VenueCode { get; init; }
    public required string Instrument { get; init; }
    public decimal Price { get; init; }
    public long OriginalQuantity { get; init; }
    public long RemainingQuantity { get; internal set; }
    
    public OrderState State { get; internal set; }
    public long SequenceNumber { get; internal set; }

    public bool Validate()
    {
        var valid = !Guid.Empty.Equals(OrderId);
        valid = valid && !string.IsNullOrWhiteSpace(VenueCode);
        valid = valid && !string.IsNullOrEmpty(Instrument);
        valid = valid && !Guid.Empty.Equals(ParticipantId);
        valid = valid && Price > 0;
        valid = valid && OriginalQuantity > 0;
        valid = valid && RemainingQuantity >= 0 && RemainingQuantity <= OriginalQuantity;
        return valid;
    }

    public override string ToString() =>
        $"{OrderId}, {VenueCode}, {ActionType}, {Instrument}, {Price}, {OriginalQuantity}, {RemainingQuantity}, {State}";
}