using System.Threading.Channels;
using ACMEMarketData.Infrastructure.Components.Participant;
using ACMEMarketData.Infrastructure.Components.Venue.Matching;

namespace ACMEMarketData.Infrastructure.Components.Venue;

public class Venue
{
    private readonly string _code;
    private readonly ChannelReader<Order> _ordersReader;
    
    // Venue core
    private readonly MatchingEngine _matchingEngine = new MatchingEngine();

    public Venue(string code, VenueOrderRouter router)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(router);
        
        _code = code;
        _ordersReader = router.GetOrderReader(_code);
    }

    public async Task StartAcceptingOrdersAsync(CancellationToken cancellationToken)
    {
        try
        {
            // CPU-intense loop
            await foreach (var order in _ordersReader.ReadAllAsync(cancellationToken))
            {
                ProcessOrder(order);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during graceful application shutdown.
        }
    }
    
    private void ProcessOrder(Order order)
    {
        Console.WriteLine(
            $"Venue={_code} Sequence={order.SequenceNumber} Order={order.OrderId}");

        // Future:
        // matchingEngine.Process(order);
        // publisher.Publish(...);
    }
}