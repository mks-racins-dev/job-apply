using System.Security.Claims;

using Microsoft.EntityFrameworkCore;

using JobMagnet.Api.Auth;
using JobMagnet.Data;
using JobMagnet.Data.Entities;

namespace JobMagnet.Api.Matches;

public static class MatchEndpoints
{
    private const int DefaultMinScore = 50;
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

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

    public record FilterCounts(int All, int Favorites, int Recent, int Dismissed);

    public record MatchPage(
        IReadOnlyList<MatchResponse> Items, int Total, int NewCount, int InterestedCount, string Filter, FilterCounts Counts, int Page, int PageSize);

    public static void MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/matches").RequireAuthorization();
        group.MapGet("/", ListAsync);
        group.MapPatch("/{id:guid}", UpdateStatusAsync);
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        JobMagnetDbContext db,
        TimeProvider timeProvider,
        int? minScore,
        int? page,
        int? pageSize,
        string? sort,
        string? dir,
        string? filter)
    {
        var userId = principal.GetUserId();
        var threshold = minScore ?? DefaultMinScore;
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(page ?? 1, 1);
        var activeFilter = MatchFilter.Normalize(filter);
        var now = timeProvider.GetUtcNow();

        var mine = db.Matches.AsNoTracking().Where(m => m.UserId == userId && m.Job!.IsActive);
        var everything = MatchFilter.Apply(mine, MatchFilter.All, threshold, now);
        var visible = MatchFilter.Apply(mine, activeFilter, threshold, now);

        var counts = new FilterCounts(
            await everything.CountAsync(),
            await MatchFilter.Apply(mine, MatchFilter.Favorites, threshold, now).CountAsync(),
            await MatchFilter.Apply(mine, MatchFilter.Recent, threshold, now).CountAsync(),
            await MatchFilter.Apply(mine, MatchFilter.Dismissed, threshold, now).CountAsync());

        // New and interested describe the whole pipeline, whichever filter is on screen.
        var newCount = await everything.CountAsync(m => m.Status == MatchStatus.New);
        var interestedCount = await everything.CountAsync(m => m.Status == MatchStatus.Interested);
        var total = await visible.CountAsync();
        var items = await MatchSorting.Apply(visible, sort, dir)
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .Select(ToResponse)
            .ToListAsync();

        return Results.Ok(new MatchPage(items, total, newCount, interestedCount, activeFilter, counts, pageNumber, size));
    }

    public static async Task<MatchResponse?> GetAsync(JobMagnetDbContext db, Guid userId, Guid matchId) =>
        await db.Matches.AsNoTracking()
            .Where(m => m.Id == matchId && m.UserId == userId)
            .Select(ToResponse)
            .SingleOrDefaultAsync();

    private static readonly System.Linq.Expressions.Expression<Func<Match, MatchResponse>> ToResponse = m => new MatchResponse(
        m.Id, m.Score, m.Reason, m.MatchedSkills, m.MissingSkills, m.Status, m.CreatedAt,
        new JobResponse(m.Job!.Id, m.Job.Title, m.Job.Company, m.Job.City, m.Job.SalaryMin, m.Job.SalaryMax, m.Job.Source, m.Job.Url));

    private static async Task<IResult> UpdateStatusAsync(Guid id, StatusUpdate update, ClaimsPrincipal principal, JobMagnetDbContext db)
    {
        var userId = principal.GetUserId();
        var updated = await db.Matches
            .Where(m => m.Id == id && m.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, update.Status));

        return updated == 0 ? Results.NotFound() : Results.NoContent();
    }
}
