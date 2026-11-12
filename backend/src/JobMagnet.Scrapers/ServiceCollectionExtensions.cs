using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using JobMagnet.Scrapers.Ingestion;
using JobMagnet.Scrapers.Sources;

namespace JobMagnet.Scrapers;

public static class ServiceCollectionExtensions
{
    private const string UserAgent = "JobMagnetBot/0.1";

    public static IServiceCollection AddJobMagnetScrapers(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ScraperOptions>(configuration.GetSection(ScraperOptions.SectionName));
        services.AddSingleton(TimeProvider.System);

        AddSource<NvaSource>(services); // first: a few fast API calls, no crawl delay
        AddSource<VisiDarbiSource>(services);
        AddSource<CvLvSource>(services);
        AddSource<SsLvSource>(services);

        services.AddScoped<JobIngestionService>();
        return services;
    }

    private static void AddSource<TSource>(IServiceCollection services)
        where TSource : class, IJobSource
    {
        services.AddHttpClient<TSource>(client => client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent));
        services.AddTransient<IJobSource>(sp => sp.GetRequiredService<TSource>());
    }
}
