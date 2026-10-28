using Anthropic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using JobMagnet.Matching.Profiles;

namespace JobMagnet.Matching;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobMagnetMatching(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ClaudeOptions>(configuration.GetSection(ClaudeOptions.SectionName));

        // The client reads ANTHROPIC_API_KEY from the environment.
        services.AddSingleton(_ => new AnthropicClient());
        services.AddSingleton<IClaudeJsonClient, ClaudeJsonClient>();
        services.AddScoped<CvProfileExtractor>();
        return services;
    }
}
