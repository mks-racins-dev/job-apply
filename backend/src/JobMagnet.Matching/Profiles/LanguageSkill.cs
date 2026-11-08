namespace JobMagnet.Matching.Profiles;

public class LanguageSkill
{
    public string Language { get; init; } = string.Empty;

    // One of CandidateProfile.LanguageLevelOptions.
    public string Level { get; init; } = "basic";
}
