using Microsoft.Extensions.Options;

using JobMagnet.Scrapers;
using JobMagnet.Scrapers.Ingestion;

namespace JobMagnet.Worker;

public class ScrapeBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ScraperOptions> options,
    ILogger<ScrapeBackgroundService> logger) : BackgroundService
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
        logger.LogInformation("Ingestion run finished: {Added} new jobs", added);
    }
}
