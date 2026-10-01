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
        var orderBookOrders = OrderBookStateFirstSet().ToArray();

        foreach (var incomingOrder in IncomingOrdersFirstSet())
            data.Add(orderBookOrders, incomingOrder.order, incomingOrder.shouldBeFulFilled);
            
        return data;
    }

    private static IEnumerable<Order> OrderBookStateFirstSet()
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

    private static IEnumerable<(Order order, bool shouldBeFulFilled)> IncomingOrdersFirstSet()
    {
        yield return (order: new Order {ActionType = ParticipantActionType.Sell, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 100m, OriginalQuantity = 100}, true);
        yield return (order: new Order {ActionType = ParticipantActionType.Buy, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 100m, OriginalQuantity = 100}, false );
        yield return (order: new Order { ActionType = ParticipantActionType.Buy, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 343m, OriginalQuantity = 10 }, true);
        yield return (order: new Order { ActionType = ParticipantActionType.Sell, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 341m, OriginalQuantity = 10}, false);
    }

    // private static IEnumerable<Order> OrderBookStateSecondSet()
    // { 
    //     return []; // Start with an empty book
    // }
    //
    // private static IEnumerable<(Order order, bool shouldBeFulFilled)> IncomingOrdersSecondSet()
    // {
    //     yield return (new Order { ActionType = ParticipantActionType.Buy, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 100, OriginalQuantity = 100}, false);
    //     yield return (new Order { ActionType = ParticipantActionType.Buy, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 99, OriginalQuantity = 50}, false);
    //     yield return (new Order { ActionType = ParticipantActionType.Sell, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 101, OriginalQuantity = 30}, false);
    //     yield return (new Order { ActionType = ParticipantActionType.Sell, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 100, OriginalQuantity = 120}, false);
    // }
}