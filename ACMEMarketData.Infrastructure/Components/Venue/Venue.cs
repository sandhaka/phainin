using System.Threading.Channels;
using ACMEMarketData.Infrastructure.Components.Participant;
using ACMEMarketData.Infrastructure.Components.Venue.Matching;

namespace ACMEMarketData.Infrastructure.Components.Venue;

public class Venue
{
    private readonly MatchingEngine _matchingEngine;
    private readonly OrderBook _orderBook;
    private readonly Channel<Order> _orders;
    
    private long _nextSequenceNumber = 0;

    public Venue()
    {
        _matchingEngine = new MatchingEngine();
        _orderBook = new OrderBook();
        
        _orders = Channel.CreateUnbounded<Order>(new UnboundedChannelOptions
        {
            SingleReader = true, // Sequentially dequeue
            SingleWriter = false // Concurrent participants can submit their orders
        });
    }

    public bool Submit(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!order.Validate())
        {
            order.State = OrderState.Rejected;
            return false;
        }

        order.SequenceNumber = Interlocked.Increment(ref _nextSequenceNumber);
        
        return _orders.Writer.TryWrite(order);
    }
}