using JobMagnet.Data;

namespace JobMagnet.Worker;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddJobMagnetData(builder.Configuration);

        var host = builder.Build();
        await host.RunAsync();
    }
}
