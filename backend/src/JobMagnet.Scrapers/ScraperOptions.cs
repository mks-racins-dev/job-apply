namespace JobMagnet.Scrapers;

public class ScraperOptions
{
    public const string SectionName = "Scrapers";

    public int MaxPagesPerSource { get; set; } = 3;

    // visidarbi.lv asks for a 10 second crawl delay in robots.txt; apply it to every source.
    public TimeSpan DelayBetweenRequests { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan RunInterval { get; set; } = TimeSpan.FromHours(2);

    public TimeSpan InactiveAfter { get; set; } = TimeSpan.FromDays(3);
}
