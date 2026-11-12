using JobMagnet.Data;
using JobMagnet.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobMagnet.Scrapers.Ingestion;

public partial class JobIngestionService(
    JobMagnetDbContext db,
    IEnumerable<IJobSource> sources,
    IOptions<ScraperOptions> options,
    TimeProvider timeProvider,
    ILogger<JobIngestionService> logger)
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Scraping {Source} failed")]
    private static partial void LogScrapeFailed(ILogger logger, Exception exception, string source);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Source}: {Added} new jobs")]
    private static partial void LogUpserted(ILogger logger, string source, int added);

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var totalNew = 0;

        foreach (var source in sources)
        {
            try
            {
                var scraped = await source.FetchAsync(cancellationToken).ConfigureAwait(false);
                totalNew += await UpsertAsync(scraped, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                // One broken site must not stop ingestion from the others.
                LogScrapeFailed(logger, ex, source.Name);
            }
        }

        await DeactivateStaleAsync(cancellationToken).ConfigureAwait(false);
        return totalNew;
    }

    public async Task<int> UpsertAsync(IReadOnlyList<ScrapedJob> scraped, CancellationToken cancellationToken)
    {
        if (scraped.Count == 0)
        {
            return 0;
        }

        var now = timeProvider.GetUtcNow();
        var source = scraped[0].Source;
        var externalIds = scraped.Select(j => j.ExternalId).ToList();
        var existing = await db.Jobs
            .Where(j => j.Source == source && externalIds.Contains(j.ExternalId))
            .ToDictionaryAsync(j => j.ExternalId, cancellationToken)
            .ConfigureAwait(false);

        var hashes = scraped.Select(DedupeHasher.Compute).ToList();
        var knownHashes = await db.Jobs
            .Where(j => j.Source != source && j.IsActive && j.SubmittedByUserId == null && hashes.Contains(j.DedupeHash))
            .Select(j => j.DedupeHash)
            .ToHashSetAsync(cancellationToken)
            .ConfigureAwait(false);

        var added = 0;
        foreach (var item in scraped.DistinctBy(j => j.ExternalId))
        {
            var hash = DedupeHasher.Compute(item);
            if (existing.TryGetValue(item.ExternalId, out var job))
            {
                job.LastSeenAt = now;
                job.IsActive = true;
                job.Deadline = item.Deadline ?? job.Deadline;
                continue;
            }

            // Cross-posted duplicate: the first source we saw it on stays canonical.
            if (knownHashes.Contains(hash))
            {
                continue;
            }

            db.Jobs.Add(ToEntity(item, hash, now));
            knownHashes.Add(hash);
            added++;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogUpserted(logger, source, added);
        return added;
    }

    private async Task DeactivateStaleAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow() - options.Value.InactiveAfter;
        await db.Jobs
            .Where(j => j.IsActive && j.SubmittedByUserId == null && j.LastSeenAt < cutoff)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.IsActive, false), cancellationToken)
            .ConfigureAwait(false);
    }

    private static Job ToEntity(ScrapedJob item, string hash, DateTimeOffset now) => new()
    {
        Source = item.Source,
        ExternalId = item.ExternalId,
        Url = item.Url,
        Title = item.Title,
        Company = item.Company,
        City = item.City,
        SalaryMin = item.SalaryMin,
        SalaryMax = item.SalaryMax,
        Description = item.Description,
        PostedAt = item.PostedAt,
        Deadline = item.Deadline,
        FirstSeenAt = now,
        LastSeenAt = now,
        DedupeHash = hash
    };
}
