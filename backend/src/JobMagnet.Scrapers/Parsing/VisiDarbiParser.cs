using AngleSharp.Html.Parser;

namespace JobMagnet.Scrapers.Parsing;

public static class VisiDarbiParser
{
    public const string SourceName = "visidarbi.lv";
    public const string BaseUrl = "https://www.visidarbi.lv";

    public static IReadOnlyList<ScrapedJob> Parse(string html)
    {
        var document = new HtmlParser().ParseDocument(html);
        var jobs = new List<ScrapedJob>();

        foreach (var item in document.QuerySelectorAll("div.item"))
        {
            var link = item.QuerySelector(".title h3 a");
            var href = link?.GetAttribute("href");
            if (link is null || string.IsNullOrEmpty(href))
            {
                continue;
            }

            var (salaryMin, salaryMax) = SalaryParser.Parse(item.QuerySelector("li.salary span")?.TextContent);

            jobs.Add(new ScrapedJob(
                Source: SourceName,
                ExternalId: href.TrimEnd('/').Split('/')[^1],
                Url: $"{BaseUrl}{href}",
                Title: link.TextContent.Trim(),
                Company: item.QuerySelector("li.company span")?.TextContent.Trim(),
                City: item.QuerySelector("li.location span")?.TextContent.Trim(),
                SalaryMin: salaryMin,
                SalaryMax: salaryMax,
                Description: string.Empty,
                PostedAt: null));
        }

        return jobs;
    }
}
