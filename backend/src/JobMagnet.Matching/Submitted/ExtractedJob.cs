namespace JobMagnet.Matching.Submitted;

public record ExtractedJob(string Title, string? Company, string? City, decimal? SalaryMin, decimal? SalaryMax, string Description);
