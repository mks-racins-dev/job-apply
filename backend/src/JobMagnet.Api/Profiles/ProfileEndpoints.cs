using System.Security.Claims;
using System.Text.Json;
using JobMagnet.Api.Auth;
using JobMagnet.Data;
using JobMagnet.Data.Entities;
using JobMagnet.Matching.Profiles;
using Microsoft.EntityFrameworkCore;

namespace JobMagnet.Api.Profiles;

public static class ProfileEndpoints
{
    public record ProfileResponse(JsonElement Data, int Version, bool IsConfirmed, DateTimeOffset UpdatedAt);

    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var profile = app.MapGroup("/profile").RequireAuthorization();
        profile.MapGet("/", GetProfileAsync);
        profile.MapPut("/", UpdateProfileAsync);
    }

    private static async Task<IResult> GetProfileAsync(ClaimsPrincipal principal, JobMagnetDbContext db)
    {
        var userId = principal.GetUserId();
        var profile = await db.Profiles.SingleOrDefaultAsync(p => p.UserId == userId);
        return profile is null ? Results.NotFound() : Results.Ok(ToResponse(profile));
    }

    // The user fills the profile in themselves, so saving it is what confirms it and starts matching.
    private static async Task<IResult> UpdateProfileAsync(
        CandidateProfile data, ClaimsPrincipal principal, JobMagnetDbContext db, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var candidate = data.Normalize();
        if (candidate.Validate() is { } error)
        {
            return Results.BadRequest(error);
        }

        var userId = principal.GetUserId();
        var profile = await db.Profiles.SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is null)
        {
            profile = new Profile { UserId = userId, Data = candidate.ToJson() };
            db.Profiles.Add(profile);
        }

        profile.Data = candidate.ToJson();
        profile.Version++;
        profile.IsConfirmed = true;
        profile.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(profile));
    }

    private static ProfileResponse ToResponse(Profile profile)
    {
        using var document = JsonDocument.Parse(profile.Data);
        return new ProfileResponse(document.RootElement.Clone(), profile.Version, profile.IsConfirmed, profile.UpdatedAt);
    }
}
