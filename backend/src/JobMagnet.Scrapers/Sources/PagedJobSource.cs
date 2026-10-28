using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobMagnet.Scrapers.Sources;

public abstract class PagedJobSource(HttpClient httpClient, IOptions<ScraperOptions> options, ILogger logger) : IJobSource
{
    private readonly ScraperOptions _options = options.Value;

    public abstract string Name { get; }

    public async Task<IReadOnlyList<ScrapedJob>> FetchAsync(CancellationToken cancellationToken)
    {
        var jobs = new List<ScrapedJob>();

        for (var page = 1; page <= _options.MaxPagesPerSource; page++)
        {
            if (page > 1)
            {
                await Task.Delay(_options.DelayBetweenRequests, cancellationToken).ConfigureAwait(false);
            }

            var url = GetPageUrl(page);
            using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("{Source} returned {StatusCode} for {Url}", Name, (int)response.StatusCode, url);
                break;
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var pageJobs = Parse(html);
            if (pageJobs.Count == 0)
            {
                break;
            }

            jobs.AddRange(pageJobs);
        }

        logger.LogInformation("{Source} fetched {Count} listings", Name, jobs.Count);
        return jobs;
    }

    protected abstract string GetPageUrl(int page);

    protected abstract IReadOnlyList<ScrapedJob> Parse(string html);
}
