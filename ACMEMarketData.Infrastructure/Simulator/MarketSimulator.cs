using System.Collections.ObjectModel;
using ACMEMarketData.Infrastructure.Components.Participant;
using ParticipantActor = ACMEMarketData.Infrastructure.Components.Participant.Participant;

namespace ACMEMarketData.Infrastructure.Simulator;

public sealed class MarketSimulator
{
    private readonly SimulationParameters _parameters;
    private readonly ReadOnlyCollection<AssetParameters> _assetParameters;

    public double OrdersPerSecond => _parameters.OrdersPerSecond;
    public int NumberOfParticipants => _parameters.NumberOfParticipants;
    
    public MarketSimulator(SimulationParameters parameters, IEnumerable<AssetParameters> assetParameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(assetParameters);

        if (parameters.NumberOfParticipants <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), "NumberOfParticipants must be greater than zero.");
        }

        if (parameters.OrdersPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), "OrdersPerSecond must be greater than zero.");
        }

        if (parameters.OrderSizeRange.Lower <= 0 ||
            parameters.OrderSizeRange.Upper < parameters.OrderSizeRange.Lower ||
            parameters.OrderSizeRange.Upper == long.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(parameters),
                "OrderSizeRange must contain positive, ascending bounds below long.MaxValue.");
        }

        _parameters = parameters;
        _assetParameters = [.. assetParameters];

        if (_assetParameters.Count == 0)
        {
            throw new ArgumentException("At least one asset must be configured.", nameof(assetParameters));
        }
    }

    public Order GenerateOrder(ParticipantActor participant, string venueCode)
    {
        ArgumentNullException.ThrowIfNull(participant);

        if (string.IsNullOrWhiteSpace(venueCode))
        {
            throw new ArgumentException("Venue code is required.", nameof(venueCode));
        }

        var (instrument, actionType, price, quantity) = GenerateRandomOrderData(Random.Shared);

        return participant.GenerateOrder(venueCode, instrument, actionType, price, quantity);
    }

    private (string Instrument, ParticipantActionType ActionType, decimal Price, long Quantity) GenerateRandomOrderData(Random random)
    {
        var instrument = _assetParameters[random.Next(_assetParameters.Count)];
        var actionType = random.Next(2) == 0
            ? ParticipantActionType.Buy
            : ParticipantActionType.Sell;
        var randomVolatility = random.NextDouble() * instrument.PriceVolatility;
        randomVolatility = random.Next(2) == 0
            ? randomVolatility          // Positive volatility
            : randomVolatility * -1;    // Negative volatility
        var price = instrument.InitialPrice + (decimal) randomVolatility;
        var quantity = random.NextInt64(_parameters.OrderSizeRange.Lower, _parameters.OrderSizeRange.Upper + 1);

        return (instrument.Symbol, actionType, Math.Round(price, 2) , quantity);
    }
}