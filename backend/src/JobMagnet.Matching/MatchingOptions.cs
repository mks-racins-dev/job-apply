namespace JobMagnet.Matching;

public class MatchingOptions
{
    public const string SectionName = "Matching";

    // Caps Claude spend per user per run; everything below the cut is never sent to the LLM.
    public int MaxCandidatesPerUser { get; set; } = 20;

    public int JobsPerScoringRequest { get; set; } = 10;

    public int NotifyThreshold { get; set; } = 70;

    public TimeSpan RunInterval { get; set; } = TimeSpan.FromMinutes(5);
}
