using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using JobMagnet.Scrapers.Parsing;

namespace JobMagnet.Scrapers.Sources;

public class VisiDarbiSource(HttpClient httpClient, IOptions<ScraperOptions> options, ILogger<VisiDarbiSource> logger)
    : PagedJobSource(httpClient, options, logger)
{
    public override string Name => VisiDarbiParser.SourceName;

    protected override string GetPageUrl(int page) => $"{VisiDarbiParser.BaseUrl}/darba-sludinajumi?page={page}";

    protected override IReadOnlyList<ScrapedJob> Parse(string html) => VisiDarbiParser.Parse(html);
}
