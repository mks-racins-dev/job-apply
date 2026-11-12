using JobMagnet.Scrapers.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobMagnet.Scrapers.Sources;

public class CvLvSource(HttpClient httpClient, IOptions<ScraperOptions> options, ILogger<CvLvSource> logger)
    : PagedJobSource(httpClient, options, logger)
{
    private const int PageSize = 20;

    public override string Name => CvLvParser.SourceName;

    protected override string GetPageUrl(int page) =>
        $"{CvLvParser.BaseUrl}/lv/search?limit={PageSize}&offset={(page - 1) * PageSize}&sorting=LATEST";

    protected override IReadOnlyList<ScrapedJob> Parse(string html) => CvLvParser.Parse(html);
}
