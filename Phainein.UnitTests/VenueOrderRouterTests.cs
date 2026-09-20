using ACMEMarketData.Infrastructure.Components.Participant;
using ACMEMarketData.Infrastructure.Components.Venue;

namespace Phainein.UnitTests;

public sealed class VenueOrderRouterTests
{
    [Fact]
    public async Task SubmitOrder_EnqueuesOrderOnlyForItsEnabledVenue()
    {
        var router = CreateRouter();
        var order = CreateOrder("XNYS");

        var submitted = router.SubmitOrder(order);

        Assert.True(submitted);
        Assert.Equal(1, order.SequenceNumber);
        Assert.Same(order, await router.GetOrderReader("XNYS").ReadAsync());
        Assert.False(router.GetOrderReader("XNAS").TryRead(out _));
    }

    [Fact]
    public void SubmitOrder_RejectsOrdersForDisabledVenues()
    {
        var router = CreateRouter();
        var order = CreateOrder("BATS");

        var submitted = router.SubmitOrder(order);

        Assert.False(submitted);
        Assert.Equal(OrderState.Rejected, order.State);
        Assert.False(router.GetOrderReader("XNYS").TryRead(out _));
    }

    [Fact]
    public void Constructor_CreatesChannelsOnlyForEnabledVenues()
    {
        var router = CreateRouter();

        Assert.Equal(["XNAS", "XNYS"], router.EnabledVenueCodes.Order().ToArray());
        Assert.Throws<KeyNotFoundException>(() => router.GetOrderReader("BATS"));
    }

    private static VenueOrderRouter CreateRouter() => new(
    [
        new VenueConfiguration { Code = "XNYS", Enabled = true },
        new VenueConfiguration { Code = "XNAS", Enabled = true },
        new VenueConfiguration { Code = "BATS", Enabled = false }
    ]);

    private static Order CreateOrder(string venueCode) => Participant.Random().GenerateOrder(
        venueCode,
        "AAPL",
        ParticipantActionType.Buy,
        100m,
        10);
}