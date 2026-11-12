using JobMagnet.Scrapers;
using JobMagnet.Scrapers.Ingestion;
using Microsoft.Extensions.Options;

namespace JobMagnet.Worker;

public partial class PipelineBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ScraperOptions> options,
    ILogger<PipelineBackgroundService> logger) : BackgroundService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Ingestion finished: {Added} new jobs")]
    private static partial void LogIngestionFinished(ILogger logger, int added);

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
        LogIngestionFinished(logger, added);
    }
}
