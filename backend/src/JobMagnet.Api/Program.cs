using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using JobMagnet.Api.Auth;
using JobMagnet.Api.Cv;
using JobMagnet.Api.Matches;
using JobMagnet.Data;
using JobMagnet.Data.Entities;
using JobMagnet.Matching;

namespace JobMagnet.Api;

public static class Program
{
    private const int MinSigningKeyLength = 32;

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddJobMagnetData(builder.Configuration);
        builder.Services.AddJobMagnetMatching(builder.Configuration);
        builder.Services.AddHealthChecks().AddDbContextCheck<JobMagnetDbContext>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        AddAuth(builder);

        var app = builder.Build();

        if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<JobMagnetDbContext>();
            await db.Database.MigrateAsync();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health");
        app.MapAuthEndpoints();
        app.MapCvEndpoints();
        app.MapMatchEndpoints();

        await app.RunAsync();
    }

    private static void AddAuth(WebApplicationBuilder builder)
    {
        var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
        var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
        if (jwt.SigningKey.Length < MinSigningKeyLength)
        {
            throw new InvalidOperationException($"Jwt:SigningKey must be at least {MinSigningKeyLength} characters.");
        }

        builder.Services.Configure<JwtOptions>(jwtSection);
        builder.Services.AddSingleton<TokenService>();
        builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        builder.Services.AddAuthorization();
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Issuer,
                    IssuerSigningKey = TokenService.CreateKey(jwt.SigningKey)
                };
            });
    }
}
