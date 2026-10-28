using Microsoft.Extensions.Options;

using JobMagnet.Matching;
using JobMagnet.Scrapers;
using JobMagnet.Scrapers.Ingestion;

namespace JobMagnet.Worker;

public class PipelineBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ScraperOptions> options,
    ILogger<PipelineBackgroundService> logger) : BackgroundService
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
        var ingestion = scope.ServiceProvider.GetRequiredService<JobIngestionService>();
        var added = await ingestion.RunAsync(stoppingToken);

        var matching = scope.ServiceProvider.GetRequiredService<MatchingService>();
        var strongMatches = await matching.RunForAllUsersAsync(stoppingToken);
        logger.LogInformation("Run finished: {Added} new jobs, {StrongMatches} strong matches", added, strongMatches);
    }
}
