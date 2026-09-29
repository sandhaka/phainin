using ACMEMarketData.Infrastructure.Components.Participant;

namespace ACMEMarketData.Tests;

public sealed class MachineEngineTests
{
    private const string Asset = "META";
    private const string Venue = "XNAS";

    private readonly Infrastructure.Components.Venue.Matching.MatchingEngine _engine = new();

    [Fact]
    public void MachineEngine_WithAnEmptyOrderBook_ShouldAddTheOrderInTheBook()
    {
        _engine.ProcessOrder(new Order 
        {  
            ActionType = ParticipantActionType.Sell, 
            ParticipantId = Guid.NewGuid(),
            VenueCode = Venue,
            Instrument = Asset,
            Price = 100m,
            OriginalQuantity = 100 
        });
            
        Assert.True(_engine.TryGetPrices(Asset, out var p));
        Assert.NotNull(p);
    }

    [Theory]
    [MemberData(nameof(TestOrdersFactory))]
    public void MachineEngine_WithAPreconfiguredOrderBook_ShouldProcessTheOrderCorrectly(
        IEnumerable<Order> previousOrders, Order incomingOrder, bool shouldBeFulfilled)
    {
        // Setup the order book
        foreach (var order in previousOrders)
        {
            // Order assumed as new
            order.RemainingQuantity = order.OriginalQuantity;
                
            _engine.ProcessOrder(order);
        }
            
        // Process incoming order
        incomingOrder.RemainingQuantity = incomingOrder.OriginalQuantity;
        _engine.ProcessOrder(incomingOrder);
            
        // Verify the order book
        Assert.True(_engine.TryGetPrices(Asset, out var p));
        Assert.NotNull(p);
            
        // Verify the order status
        Assert.Equal(shouldBeFulfilled, incomingOrder.FulFilled);
    }

    public static TheoryData<IEnumerable<Order>, Order, bool> TestOrdersFactory()
    {
        var data = new TheoryData<IEnumerable<Order>, Order, bool>();
        var orderBookOrders = PrepareOrderBook().ToArray();

        foreach (var incomingOrder in PrepareIncomingOrders())
            data.Add(orderBookOrders, incomingOrder.order, incomingOrder.shouldBeFulfilled);
            
        return data;
    }

    private static IEnumerable<Order> PrepareOrderBook()
    {
        return new List<Order>
        {
            new()
            {
                ActionType = ParticipantActionType.Buy, 
                Instrument = Asset, 
                OriginalQuantity = 100, 
                ParticipantId = Guid.NewGuid(),
                VenueCode = Venue,
                Price = 100m
            },
            new()
            {
                ActionType = ParticipantActionType.Buy, 
                Instrument = Asset, 
                OriginalQuantity = 50, 
                ParticipantId = Guid.NewGuid(),
                VenueCode = Venue,
                Price = 99m
            },
            new()
            {
                ActionType = ParticipantActionType.Sell, 
                Instrument = Asset, 
                OriginalQuantity = 80, 
                ParticipantId = Guid.NewGuid(),
                VenueCode = Venue,
                Price = 101m
            }
        };
    }

    private static IEnumerable<(Order order, bool shouldBeFulfilled)> PrepareIncomingOrders()
    {
        yield return (
            order: new Order
            {
                ActionType = ParticipantActionType.Sell,
                ParticipantId = Guid.NewGuid(),
                VenueCode = Venue,
                Instrument = Asset,
                Price = 100m,
                OriginalQuantity = 100
            }, 
            shouldBeFulfilled: true
        );
            
        yield return (
            order: new Order
            {
                ActionType = ParticipantActionType.Buy,
                ParticipantId = Guid.NewGuid(),
                VenueCode = Venue,
                Instrument = Asset,
                Price = 100m,
                OriginalQuantity = 100
            }, 
            shouldBeFulfilled: false
        );
    }
}