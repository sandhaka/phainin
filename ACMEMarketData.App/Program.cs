using ACMEMarketData.App;
using ACMEMarketData.Infrastructure.Components.Venue;
using ACMEMarketData.Infrastructure.Simulator;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var simulationParameters = configuration
    .GetRequiredSection("SimulationParameters")
    .Get<SimulationParameters>()
    ?? throw new InvalidOperationException("SimulationParameters could not be loaded.");

var assets = configuration.GetSection("Assets").Get<List<AssetParameters>>() ?? [];
var marketParameters = configuration
    .GetRequiredSection("Market")
    .Get<MarketParameters>()
    ?? throw new InvalidOperationException("Market parameters could not be loaded.");

var participantParameters = configuration
    .GetRequiredSection("Participants")
    .Get<ParticipantParameters>()
    ?? throw new InvalidOperationException("Participant parameters could not be loaded.");

var threadingParameters = configuration.GetSection("Threading").Get<ThreadingParameters>() ?? new ThreadingParameters();
var venuesConfigurations = configuration.GetRequiredSection("Venues").Get<List<VenueConfiguration>>()
    ?? throw new InvalidOperationException("Venue configuration could not be loaded.");

venuesConfigurations = [.. venuesConfigurations.Where(c => c.Enabled)];

var marketSimulator = new MarketSimulator(simulationParameters, assets);
var venueOrderRouter = new VenueOrderRouter(venuesConfigurations);
var venues = venuesConfigurations.Select(c => new Venue(c.Code, venueOrderRouter)).ToList();

Console.WriteLine("ACME Market Data simulation configuration loaded.");
Console.WriteLine($"> Simultaneous participants: {simulationParameters.NumberOfParticipants}");
Console.WriteLine($"> Orders per second: {simulationParameters.OrdersPerSecond}");
Console.WriteLine($"> Total Assets: {assets.Count}");
Console.WriteLine($"> Enabled venues: {string.Join(", ", venueOrderRouter.EnabledVenueCodes)}");

if (!AppPreconditions()) return -1;

var orderGeneratorWorker = new OrderGeneratorWorker(marketSimulator, venueOrderRouter);

// Cancellation token to control the order generations
using var orderGenCancellationTokenSource = new CancellationTokenSource();

// Application level control
using var applicationCancellationTokenSource = new CancellationTokenSource();

var generatorTask = orderGeneratorWorker.StartAsync(orderGenCancellationTokenSource.Token);
var venuesWorkerTasks = venues.Select(v => v.StartAcceptingOrdersAsync(applicationCancellationTokenSource.Token))
    .ToArray();

await Task.Delay(TimeSpan.FromSeconds(5));

// Stop orders generation
await orderGenCancellationTokenSource.CancelAsync();

// Wait termination gracefully
await generatorTask;

// Wait full orders processing completion
venueOrderRouter.Complete();

await Task.WhenAll(venuesWorkerTasks);
return 0;

bool AppPreconditions()
{
    if (!venues.Any())
    {
        Console.WriteLine("At least one venue should be enabled");
        return false;
    }

    if (simulationParameters.NumberOfParticipants <= 0)
    {
        Console.WriteLine("At least one participant should be used");
        return false;
    }

    if (assets.Count == 0)
    {
        Console.WriteLine("At least one asset should be configured");
        return false;
    }
    
    return true;
}