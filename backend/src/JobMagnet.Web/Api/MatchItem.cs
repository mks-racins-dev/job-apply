namespace JobMagnet.Web.Api;

public record MatchItem(
    Guid Id,
    int Score,
    string Reason,
    List<string> MatchedSkills,
    List<string> MissingSkills,
    MatchStatus Status,
    DateTimeOffset CreatedAt,
    JobItem Job);
