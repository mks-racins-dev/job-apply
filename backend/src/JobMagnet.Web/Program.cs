using Microsoft.AspNetCore.DataProtection;

using JobMagnet.Web.Api;
using JobMagnet.Web.Components;
using JobMagnet.Web.Session;

namespace JobMagnet.Web;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        // Keys encrypt the stored session and antiforgery tokens; losing them on a redeploy signs everyone out.
        var dataProtection = builder.Services.AddDataProtection().SetApplicationName("JobMagnet.Web");
        if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        builder.Services.AddScoped<AuthSession>();
        builder.Services.AddHttpClient<JobMagnetApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl));

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        await app.RunAsync();
    }
}
