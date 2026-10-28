using System.Security.Claims;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using JobMagnet.Api.Auth;
using JobMagnet.Data;
using JobMagnet.Data.Entities;
using JobMagnet.Matching.Profiles;

namespace JobMagnet.Api.Cv;

public static class CvEndpoints
{
    private const long MaxCvBytes = 10 * 1024 * 1024;

    public record ProfileResponse(JsonElement Data, int Version, bool IsConfirmed, DateTimeOffset UpdatedAt);

    public static void MapCvEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/cv", UploadAsync).RequireAuthorization().DisableAntiforgery();

        var profile = app.MapGroup("/profile").RequireAuthorization();
        profile.MapGet("/", GetProfileAsync);
        profile.MapPut("/", UpdateProfileAsync);
    }

    private static async Task<IResult> UploadAsync(
        IFormFile file,
        ClaimsPrincipal principal,
        JobMagnetDbContext db,
        CvProfileExtractor extractor,
        IConfiguration configuration,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0 || file.Length > MaxCvBytes || !CvTextExtractor.IsSupported(file.FileName))
        {
            return Results.BadRequest("Upload a PDF or DOCX file up to 10 MB.");
        }

        var userId = principal.GetUserId();
        var storagePath = await SaveFileAsync(file, userId, configuration, cancellationToken);

        await using var stream = file.OpenReadStream();
        var text = CvTextExtractor.Extract(stream, file.FileName);
        if (string.IsNullOrWhiteSpace(text))
        {
            // Scanned CVs have no text layer; OCR is an open question in the plan.
            return Results.UnprocessableEntity("No text could be read from this file.");
        }

        var now = timeProvider.GetUtcNow();
        db.CvDocuments.Add(new CvDocument
        {
            UserId = userId,
            FileName = file.FileName,
            StoragePath = storagePath,
            ExtractedText = text,
            UploadedAt = now
        });

        var profileJson = await extractor.ExtractAsync(text, cancellationToken);
        if (profileJson is null)
        {
            await db.SaveChangesAsync(cancellationToken);
            return Results.UnprocessableEntity("The CV could not be turned into a profile.");
        }

        var profile = await UpsertProfileAsync(db, userId, profileJson, isConfirmed: false, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(profile));
    }

    private static async Task<IResult> GetProfileAsync(ClaimsPrincipal principal, JobMagnetDbContext db)
    {
        var userId = principal.GetUserId();
        var profile = await db.Profiles.SingleOrDefaultAsync(p => p.UserId == userId);
        return profile is null ? Results.NotFound() : Results.Ok(ToResponse(profile));
    }

    // The user's reviewed edit confirms the profile, which is what lets matching start.
    private static async Task<IResult> UpdateProfileAsync(
        JsonElement data, ClaimsPrincipal principal, JobMagnetDbContext db, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        if (data.ValueKind != JsonValueKind.Object)
        {
            return Results.BadRequest("Profile must be a JSON object.");
        }

        var profile = await UpsertProfileAsync(
            db, principal.GetUserId(), data.GetRawText(), isConfirmed: true, timeProvider.GetUtcNow(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(profile));
    }

    private static async Task<Profile> UpsertProfileAsync(
        JobMagnetDbContext db, Guid userId, string json, bool isConfirmed, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var profile = await db.Profiles.SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is null)
        {
            profile = new Profile { UserId = userId, Data = json };
            db.Profiles.Add(profile);
        }

        profile.Data = json;
        profile.Version++;
        profile.IsConfirmed = isConfirmed;
        profile.UpdatedAt = now;
        return profile;
    }

    private static async Task<string> SaveFileAsync(
        IFormFile file, Guid userId, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var root = configuration["Storage:UploadsPath"] ?? "uploads";
        var directory = Path.Combine(root, userId.ToString());
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}");
        await using var target = File.Create(path);
        await file.CopyToAsync(target, cancellationToken);
        return path;
    }

    private static ProfileResponse ToResponse(Profile profile)
    {
        using var document = JsonDocument.Parse(profile.Data);
        return new ProfileResponse(document.RootElement.Clone(), profile.Version, profile.IsConfirmed, profile.UpdatedAt);
    }
}
