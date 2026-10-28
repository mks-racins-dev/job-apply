namespace JobMagnet.Web.Api;

public record JobItem(
    Guid Id, string Title, string? Company, string? City, decimal? SalaryMin, decimal? SalaryMax, string Source, string Url);
