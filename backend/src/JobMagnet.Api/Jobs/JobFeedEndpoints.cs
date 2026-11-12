using Microsoft.EntityFrameworkCore;

using JobMagnet.Data;

namespace JobMagnet.Api.Jobs;

public static class JobFeedEndpoints
{
    private static readonly TimeSpan RecentWindow = TimeSpan.FromHours(24);

    public record SourceFeed(string Source, DateTimeOffset? LastAddedAt, int AddedLastDay, int Active);

    public record FeedResponse(DateTimeOffset? LastCheckedAt, DateTimeOffset? LastAddedAt, IReadOnlyList<SourceFeed> Sources);

    public static void MapJobFeedEndpoints(this IEndpointRouteBuilder app) =>
        app.MapGet("/jobs/feed", GetFeedAsync).RequireAuthorization();

    // Scraped jobs only: jobs users paste in are private and say nothing about how fresh the sites are.
    private static async Task<IResult> GetFeedAsync(JobMagnetDbContext db, TimeProvider timeProvider)
    {
        var since = timeProvider.GetUtcNow() - RecentWindow;

        var sources = await db.Jobs.AsNoTracking()
            .Where(j => j.SubmittedByUserId == null)
            .GroupBy(j => j.Source)
            .Select(g => new
            {
                Source = g.Key,
                LastAddedAt = g.Max(j => (DateTimeOffset?)j.FirstSeenAt),
                LastCheckedAt = g.Max(j => (DateTimeOffset?)j.LastSeenAt),
                AddedLastDay = g.Count(j => j.FirstSeenAt >= since),
                Active = g.Count(j => j.IsActive)
            })
            .OrderBy(s => s.Source)
            .ToListAsync();

        return Results.Ok(new FeedResponse(
            sources.Max(s => s.LastCheckedAt),
            sources.Max(s => s.LastAddedAt),
            sources.Select(s => new SourceFeed(s.Source, s.LastAddedAt, s.AddedLastDay, s.Active)).ToList()));
    }
}
