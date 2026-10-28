namespace JobMagnet.Data.Entities;

public class Match
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Guid JobId { get; set; }

    public Job? Job { get; set; }

    public int Score { get; set; }

    public required string Reason { get; set; }

    public List<string> MatchedSkills { get; set; } = [];

    public List<string> MissingSkills { get; set; } = [];

    public MatchStatus Status { get; set; } = MatchStatus.New;

    public DateTimeOffset CreatedAt { get; set; }
}
