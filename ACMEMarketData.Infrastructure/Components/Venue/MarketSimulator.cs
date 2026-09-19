using ACMEMarketData.Infrastructure.Components.Participant;
using ParticipantActor = ACMEMarketData.Infrastructure.Components.Participant.Participant;

namespace ACMEMarketData.Infrastructure.Components.Venue;

internal sealed class MarketSimulator
{
    private static readonly string[] Instruments = ["ACME", "BETA", "GAMMA"];
    private readonly SimulationSettings _settings;

    public MarketSimulator(SimulationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.NumberOfParticipants <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "NumberOfParticipants must be greater than zero.");
        }

        if (settings.Rate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "Rate must be greater than zero.");
        }

        if (settings.OrderSizeRange.Lower <= 0 ||
            settings.OrderSizeRange.Upper < settings.OrderSizeRange.Lower ||
            settings.OrderSizeRange.Upper == long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                "OrderSizeRange must contain positive, ascending bounds below long.MaxValue.");
        }

        _settings = settings;
    }

    public Order GenerateOrder(ParticipantActor participant)
    {
        return GenerateOrder(participant, Random.Shared);
    }

    internal Order GenerateOrder(ParticipantActor participant, Random random)
    {
        ArgumentNullException.ThrowIfNull(participant);

        var (instrument, actionType, price, quantity) = GenerateRandomOrderData(random);

        return participant.GenerateOrder(instrument, actionType, price, quantity);
    }

    private (string Instrument, ParticipantActionType ActionType, decimal Price, long Quantity)
        GenerateRandomOrderData(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var instrument = Instruments[random.Next(Instruments.Length)];
        var actionType = random.Next(2) == 0
            ? ParticipantActionType.Buy
            : ParticipantActionType.Sell;
        var price = random.Next(9_500, 10_501) / 100m;
        var quantity = random.NextInt64(_settings.OrderSizeRange.Lower, _settings.OrderSizeRange.Upper + 1);

        return (instrument, actionType, price, quantity);
    }
}