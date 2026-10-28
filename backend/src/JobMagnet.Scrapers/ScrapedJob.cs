namespace JobMagnet.Scrapers;

public record ScrapedJob(
    string Source,
    string ExternalId,
    string Url,
    string Title,
    string? Company,
    string? City,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string Description,
    DateTimeOffset? PostedAt);
