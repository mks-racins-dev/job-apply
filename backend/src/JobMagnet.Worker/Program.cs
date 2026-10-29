using JobMagnet.Data;
using JobMagnet.Matching;
using JobMagnet.Scrapers;

namespace JobMagnet.Worker;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddJobMagnetData(builder.Configuration);
        builder.Services.AddJobMagnetScrapers(builder.Configuration);
        builder.Services.AddJobMagnetMatching(builder.Configuration);
        builder.Services.AddHostedService<PipelineBackgroundService>();
        builder.Services.AddHostedService<MatchingBackgroundService>();

        var host = builder.Build();
        await host.RunAsync();
    }
}
