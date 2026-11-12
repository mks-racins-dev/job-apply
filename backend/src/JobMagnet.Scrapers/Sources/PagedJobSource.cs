using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobMagnet.Scrapers.Sources;

public abstract partial class PagedJobSource(HttpClient httpClient, IOptions<ScraperOptions> options, ILogger logger) : IJobSource
{
    private readonly ScraperOptions _options = options.Value;

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Source} returned {StatusCode} for {Url}")]
    private static partial void LogBadStatus(ILogger logger, string source, int statusCode, string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Source} fetched {Count} listings")]
    private static partial void LogFetched(ILogger logger, string source, int count);

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
                LogBadStatus(logger, Name, (int)response.StatusCode, url);
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

        LogFetched(logger, Name, jobs.Count);
        return jobs;
    }

    protected abstract string GetPageUrl(int page);

    protected abstract IReadOnlyList<ScrapedJob> Parse(string html);
}
