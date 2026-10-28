using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobMagnet.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobMagnetData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        return services.AddDbContext<JobMagnetDbContext>(options => options.UseNpgsql(connectionString));
    }
}
