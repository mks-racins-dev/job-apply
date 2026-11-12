using Microsoft.Extensions.Options;

using JobMagnet.Matching;

namespace JobMagnet.Worker;

// Runs on its own short interval so a freshly confirmed profile gets matches within minutes,
// not after the next scrape. Repeat runs are cheap: already-scored jobs never go back to Claude.
public class MatchingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<MatchingOptions> options,
    ILogger<MatchingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.RunInterval);

        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var matching = scope.ServiceProvider.GetRequiredService<MatchingService>();
        var strongMatches = await matching.RunForAllUsersAsync(stoppingToken);
        if (strongMatches > 0)
        {
            logger.LogInformation("Matching finished: {StrongMatches} strong matches", strongMatches);
        }
    }
}
