using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

using JobMagnet.Api.Auth;
using JobMagnet.Data;
using JobMagnet.Data.Entities;

namespace JobMagnet.Api.Matches;

public static class MatchEndpoints
{
    private const int DefaultMinScore = 50;
    private const int MaxResults = 100;

    public record MatchResponse(
        Guid Id,
        int Score,
        string Reason,
        IReadOnlyList<string> MatchedSkills,
        IReadOnlyList<string> MissingSkills,
        MatchStatus Status,
        DateTimeOffset CreatedAt,
        JobResponse Job);

    public record JobResponse(
        Guid Id, string Title, string? Company, string? City, decimal? SalaryMin, decimal? SalaryMax, string Source, string Url);

    public record StatusUpdate(MatchStatus Status);

    public static void MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/matches").RequireAuthorization();
        group.MapGet("/", ListAsync);
        group.MapPatch("/{id:guid}", UpdateStatusAsync);
    }

    private static async Task<IResult> ListAsync(ClaimsPrincipal principal, JobMagnetDbContext db, int? minScore)
    {
        var userId = principal.GetUserId();
        var threshold = minScore ?? DefaultMinScore;

        var matches = await db.Matches.AsNoTracking()
            .Where(m => m.UserId == userId && m.Score >= threshold && m.Status != MatchStatus.Dismissed && m.Job!.IsActive)
            .OrderByDescending(m => m.Score)
            .ThenByDescending(m => m.CreatedAt)
            .Take(MaxResults)
            .Select(m => new MatchResponse(
                m.Id, m.Score, m.Reason, m.MatchedSkills, m.MissingSkills, m.Status, m.CreatedAt,
                new JobResponse(m.Job!.Id, m.Job.Title, m.Job.Company, m.Job.City, m.Job.SalaryMin, m.Job.SalaryMax, m.Job.Source, m.Job.Url)))
            .ToListAsync();

        return Results.Ok(matches);
    }

    private static async Task<IResult> UpdateStatusAsync(Guid id, StatusUpdate update, ClaimsPrincipal principal, JobMagnetDbContext db)
    {
        var userId = principal.GetUserId();
        var updated = await db.Matches
            .Where(m => m.Id == id && m.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, update.Status));

        return updated == 0 ? Results.NotFound() : Results.NoContent();
    }
}
