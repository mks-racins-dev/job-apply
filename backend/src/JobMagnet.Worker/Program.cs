using JobMagnet.Data;
using JobMagnet.Scrapers;

namespace JobMagnet.Worker;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddJobMagnetData(builder.Configuration);
        builder.Services.AddJobMagnetScrapers(builder.Configuration);
        builder.Services.AddHostedService<ScrapeBackgroundService>();

        var host = builder.Build();
        await host.RunAsync();
    }
}
