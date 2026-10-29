namespace JobMagnet.Data.Entities;

public class Job
{
    public Guid Id { get; set; }

    public required string Source { get; set; }

    public required string ExternalId { get; set; }

    public required string Url { get; set; }

    public required string Title { get; set; }

    public string? Company { get; set; }

    public string? City { get; set; }

    public decimal? SalaryMin { get; set; }

    public decimal? SalaryMax { get; set; }

    public string? Language { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset? PostedAt { get; set; }

    // When we first scraped (or the user pasted) it, as opposed to the site's own posting date.
    public DateTimeOffset FirstSeenAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Set for jobs a user pasted in themselves (e.g. from LinkedIn): only that user is matched against them.
    public Guid? SubmittedByUserId { get; set; }

    // Content hash shared by the same listing posted on several sites.
    public required string DedupeHash { get; set; }
}
