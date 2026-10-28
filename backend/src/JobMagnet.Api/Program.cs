using Microsoft.EntityFrameworkCore;

using JobMagnet.Data;

namespace JobMagnet.Api;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddJobMagnetData(builder.Configuration);
        builder.Services.AddHealthChecks().AddDbContextCheck<JobMagnetDbContext>();

        var app = builder.Build();

        if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<JobMagnetDbContext>();
            await db.Database.MigrateAsync();
        }

        app.MapHealthChecks("/health");

        await app.RunAsync();
    }
}
