using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using JobMagnet.Scrapers.Parsing;

namespace JobMagnet.Scrapers.Sources;

// Reads NVA vacancies through the data.gov.lv CKAN datastore API instead of scraping HTML.
public partial class NvaSource(HttpClient httpClient, IOptions<ScraperOptions> options, ILogger<NvaSource> logger) : IJobSource
{
    private const int PageSize = 1000;
    private readonly ScraperOptions _options = options.Value;

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Source}: no datastore resource found for dataset {Dataset}")]
    private static partial void LogNoResource(ILogger logger, string source, string dataset);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Source} fetched {Count} listings")]
    private static partial void LogFetched(ILogger logger, string source, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Source} returned {StatusCode} for {Url}")]
    private static partial void LogBadStatus(ILogger logger, string source, int statusCode, string url);

    public string Name => NvaParser.SourceName;

    public async Task<IReadOnlyList<ScrapedJob>> FetchAsync(CancellationToken cancellationToken)
    {
        var packageJson = await GetAsync($"{NvaParser.ApiBaseUrl}/package_show?id={NvaParser.DatasetId}", cancellationToken).ConfigureAwait(false);
        var resourceId = packageJson is null ? null : NvaParser.ParseResourceId(packageJson);
        if (resourceId is null)
        {
            LogNoResource(logger, Name, NvaParser.DatasetId);
            return [];
        }

        var jobs = new List<ScrapedJob>();
        for (var page = 0; page < _options.MaxPagesPerSource; page++)
        {
            var url = $"{NvaParser.ApiBaseUrl}/datastore_search?resource_id={resourceId}&limit={PageSize}&offset={page * PageSize}";
            var json = await GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (json is null)
            {
                break;
            }

            var result = NvaParser.Parse(json);
            jobs.AddRange(result.Jobs);
            if (result.Jobs.Count < PageSize || jobs.Count >= result.Total)
            {
                break;
            }
        }

        LogFetched(logger, Name, jobs.Count);
        return jobs;
    }

    private async Task<string?> GetAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            LogBadStatus(logger, Name, (int)response.StatusCode, url);
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
