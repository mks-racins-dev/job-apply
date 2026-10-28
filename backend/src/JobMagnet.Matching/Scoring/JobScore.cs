namespace JobMagnet.Matching.Scoring;

public record JobScore(Guid JobId, int Score, string Reason, List<string> MatchedSkills, List<string> MissingSkills);
