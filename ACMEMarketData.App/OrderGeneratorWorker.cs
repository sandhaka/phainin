using ACMEMarketData.Infrastructure.Components.Participant;
using ACMEMarketData.Infrastructure.Components.Venue;
using ACMEMarketData.Infrastructure.Simulator;

namespace ACMEMarketData.App;

public sealed class OrderGeneratorWorker
{
    private readonly MarketSimulator _simulator;
    private readonly IReadOnlyList<Participant> _participants;
    private readonly IReadOnlyList<string> _enabledVenueCodes;
    private readonly VenueOrderRouter _venueOrderRouter;

    public OrderGeneratorWorker(
        MarketSimulator marketSimulator,
        VenueOrderRouter venueOrderRouter)
    {
        ArgumentNullException.ThrowIfNull(marketSimulator);
        ArgumentNullException.ThrowIfNull(venueOrderRouter);

        _simulator = marketSimulator;
        _venueOrderRouter = venueOrderRouter;
        _enabledVenueCodes = venueOrderRouter.EnabledVenueCodes.ToList();
        _participants = Enumerable
            .Range(1, _simulator.NumberOfParticipants)
            .Select(index => Participant.Random($"Participant-{index}"))
            .ToList();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds(1d / _simulator.OrdersPerSecond);
        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var participant = _participants[Random.Shared.Next(_participants.Count)];
                var venueCode = _enabledVenueCodes[Random.Shared.Next(_enabledVenueCodes.Count)];
                var order = _simulator.GenerateOrder(participant, venueCode);
                var submitted = _venueOrderRouter.SubmitOrder(order);

                Console.WriteLine($"Participant={participant.Name} Id={participant.Id} Submitted={submitted} Order={order}");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }
}