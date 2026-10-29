namespace JobMagnet.Matching.Submitted;

public record SubmitJobResult(SubmitJobStatus Status, Guid? MatchId = null);
