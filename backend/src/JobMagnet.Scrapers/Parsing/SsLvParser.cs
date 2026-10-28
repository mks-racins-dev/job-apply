using AngleSharp.Html.Parser;

namespace JobMagnet.Scrapers.Parsing;

public static class SsLvParser
{
    public const string SourceName = "ss.lv";
    public const string BaseUrl = "https://www.ss.lv";

    private const string RowIdPrefix = "tr_";
    private const int ProfessionColumnIndex = 3;
    private const int TitleMaxLength = 120;

    public static IReadOnlyList<ScrapedJob> Parse(string html)
    {
        var document = new HtmlParser().ParseDocument(html);
        var jobs = new List<ScrapedJob>();

        foreach (var row in document.QuerySelectorAll($"tr[id^='{RowIdPrefix}']"))
        {
            var link = row.QuerySelector("a.am");
            var href = link?.GetAttribute("href");
            if (link is null || string.IsNullOrEmpty(href))
            {
                continue;
            }

            var cells = row.QuerySelectorAll("td");
            var profession = cells.Length > ProfessionColumnIndex ? cells[ProfessionColumnIndex].TextContent.Trim() : null;
            var text = link.TextContent.Trim();

            jobs.Add(new ScrapedJob(
                Source: SourceName,
                ExternalId: row.Id![RowIdPrefix.Length..],
                Url: $"{BaseUrl}{href}",
                Title: string.IsNullOrEmpty(profession) ? Truncate(text) : profession,
                Company: null,
                City: row.QuerySelector(".ads_region")?.TextContent.Split(',')[0].Trim(),
                SalaryMin: null,
                SalaryMax: null,
                Description: text,
                PostedAt: null));
        }

        return jobs;
    }

    private static string Truncate(string text) => text.Length <= TitleMaxLength ? text : text[..TitleMaxLength];
}
