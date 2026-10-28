using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using JobMagnet.Scrapers.Parsing;

namespace JobMagnet.Scrapers.Sources;

public class SsLvSource(HttpClient httpClient, IOptions<ScraperOptions> options, ILogger<SsLvSource> logger)
    : PagedJobSource(httpClient, options, logger)
{
    private const string ListingPath = "/lv/work/are-required/";

    public override string Name => SsLvParser.SourceName;

    protected override string GetPageUrl(int page) =>
        page == 1 ? $"{SsLvParser.BaseUrl}{ListingPath}" : $"{SsLvParser.BaseUrl}{ListingPath}page{page}.html";

    protected override IReadOnlyList<ScrapedJob> Parse(string html) => SsLvParser.Parse(html);
}
