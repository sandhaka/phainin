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

    [Theory, MemberData(nameof(TestOrdersFactory))]
    public void MachineEngine_Preliminary_WithAPreconfiguredOrderBook_ShouldProcessTheOrderCorrectly(
        IEnumerable<Order> previousOrders, Order incomingOrder, bool shouldBeFulfilled)
    {
        // Setup the order book
        foreach (var order in previousOrders)
        {
            _engine.ProcessOrder(order);
        }
        
        _engine.ProcessOrder(incomingOrder);
            
        // Verify the order book
        Assert.True(_engine.TryGetPrices(Asset, out var p));
        Assert.NotNull(p);
            
        // Verify the order status
        Assert.Equal(shouldBeFulfilled, incomingOrder.Fulfilled);
        
        if (incomingOrder.Fulfilled)
            Assert.Equal(OrderState.Completed, incomingOrder.State);
    }

    [Theory, MemberData(nameof(OrderSequence1))]
    public void MachineEngine_ShouldProcessOrderSequence_Sequence1(Queue<Order> sequence)
    {
        while (sequence.Count != 0)
        {
            var order = sequence.Dequeue();
            _engine.ProcessOrder(order);
        }

        Assert.True(_engine.TryGetPrices(Asset, out var prices));
        
        // Verify expected book and order status
        Assert.Equal(1, prices.Buy.Count);
        Assert.Equal(1, prices.Sell.Count);
        Assert.Equal(99m, prices.Buy.First().Key);
        Assert.Equal(50, prices.Buy[99m].First().RemainingQuantity);
        Assert.Equal(100m, prices.Sell.First().Key);
        Assert.Equal(40, prices.Sell[100m].First().RemainingQuantity);
        
        // TODO: Verify emitted trades
    }

    public static TheoryData<IEnumerable<Order>, Order, bool> TestOrdersFactory()
    {
        var data = new TheoryData<IEnumerable<Order>, Order, bool>();

        foreach (var incomingOrder in IncomingOrdersSet())
            data.Add(OrderBookStateSet().ToArray(), incomingOrder.order, incomingOrder.shouldBeFulFilled);
            
        return data;
    }

    private static IEnumerable<Order> OrderBookStateSet()
    {
        return new List<Order>
        {
            new() {ActionType = ParticipantActionType.Buy, Instrument = Asset, OriginalQuantity = 100, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Price = 100m},
            new() {ActionType = ParticipantActionType.Buy, Instrument = Asset, OriginalQuantity = 50, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Price = 99m},
            new() {ActionType = ParticipantActionType.Sell, Instrument = Asset, OriginalQuantity = 80, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Price = 101m}
        };
    }

    private static IEnumerable<(Order order, bool shouldBeFulFilled)> IncomingOrdersSet()
    {
        yield return (order: new Order { ActionType = ParticipantActionType.Sell, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 100m, OriginalQuantity = 100}, true);
        yield return (order: new Order { ActionType = ParticipantActionType.Buy, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 100m, OriginalQuantity = 100}, false );
        yield return (order: new Order { ActionType = ParticipantActionType.Buy, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 343m, OriginalQuantity = 10 }, true);
        yield return (order: new Order { ActionType = ParticipantActionType.Sell, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 341m, OriginalQuantity = 10}, false);
    }
    
    public static TheoryData<Queue<Order>> OrderSequence1()
    {
        var data = new TheoryData<Queue<Order>>();
        
        var q = new Queue<Order>();
        
        q.Enqueue(new Order { ActionType = ParticipantActionType.Buy, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 100m, OriginalQuantity = 100 });
        q.Enqueue(new Order { ActionType = ParticipantActionType.Buy, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 99m, OriginalQuantity = 50 });
        q.Enqueue(new Order { ActionType = ParticipantActionType.Sell, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 101m, OriginalQuantity = 30 });
        q.Enqueue(new Order { ActionType = ParticipantActionType.Sell, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 100m, OriginalQuantity = 120 });
        q.Enqueue(new Order { ActionType = ParticipantActionType.Buy, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 101m, OriginalQuantity = 40 });
        q.Enqueue(new Order { ActionType = ParticipantActionType.Buy, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 102m, OriginalQuantity = 100 });
        q.Enqueue(new Order { ActionType = ParticipantActionType.Sell, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 99m, OriginalQuantity = 80 });
        q.Enqueue(new Order { ActionType = ParticipantActionType.Sell, ParticipantId = Guid.NewGuid(), VenueCode = Venue, Instrument = Asset, Price = 100m, OriginalQuantity = 50 });

        data.Add(q);
        
        return data;
    }
}