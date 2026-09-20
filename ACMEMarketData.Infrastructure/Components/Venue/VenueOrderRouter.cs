using System.Threading.Channels;
using ACMEMarketData.Infrastructure.Components.Participant;

namespace ACMEMarketData.Infrastructure.Components.Venue;

public sealed class VenueOrderRouter
{
    private readonly Dictionary<string, Channel<Order>> _orderChannels;
    private readonly Dictionary<string, long> _nextSequenceNumbers;
    private readonly Lock _sequenceNumberLock = new();

    public IReadOnlyCollection<string> EnabledVenueCodes => _orderChannels.Keys;

    public VenueOrderRouter(IEnumerable<VenueConfiguration> venues)
    {
        ArgumentNullException.ThrowIfNull(venues);

        _orderChannels = new Dictionary<string, Channel<Order>>(StringComparer.OrdinalIgnoreCase);
        _nextSequenceNumbers = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        foreach (var venue in venues)
        {
            ArgumentNullException.ThrowIfNull(venue);

            if (string.IsNullOrWhiteSpace(venue.Code))
            {
                throw new ArgumentException("Every venue must have a code.", nameof(venues));
            }

            if (!_nextSequenceNumbers.TryAdd(venue.Code, 0))
            {
                throw new ArgumentException($"Venue code '{venue.Code}' is configured more than once.", nameof(venues));
            }

            if (!venue.Enabled)
            {
                continue;
            }

            _orderChannels.Add(venue.Code, Channel.CreateUnbounded<Order>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            }));
        }

        if (_orderChannels.Count == 0)
        {
            throw new ArgumentException("At least one venue must be enabled.", nameof(venues));
        }
    }

    public bool SubmitOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (!order.Validate() || !_orderChannels.TryGetValue(order.VenueCode, out var venueChannel))
        {
            order.State = OrderState.Rejected;
            return false;
        }

        lock (_sequenceNumberLock)
        {
            order.SequenceNumber = ++_nextSequenceNumbers[order.VenueCode];
        }

        return venueChannel.Writer.TryWrite(order);
    }

    public ChannelReader<Order> GetOrderReader(string venueCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(venueCode);

        return _orderChannels.TryGetValue(venueCode, out var orders)
            ? orders.Reader
            : throw new KeyNotFoundException($"Venue '{venueCode}' is not enabled for this run.");
    }
    
    public void Complete()
    {
        foreach (var venueCode in EnabledVenueCodes)
        {
            _orderChannels[venueCode].Writer.TryComplete();
        }
    }
}