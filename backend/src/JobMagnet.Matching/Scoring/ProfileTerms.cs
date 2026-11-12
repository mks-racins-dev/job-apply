using JobMagnet.Matching.Profiles;

namespace JobMagnet.Matching.Scoring;

public record ProfileTerms(IReadOnlyList<string> Keywords, IReadOnlyList<string> Cities, bool Remote, IReadOnlyList<string> Avoid)
{
    private const int MinRoleWordLength = 4;

    public static ProfileTerms FromProfile(CandidateProfile profile)
    {
        // Past and wanted roles count the same, so someone moving from IT to accounting still sees accounting jobs.
        var roles = profile.Experience.Select(e => e.Role).Concat(profile.InterestedRoles).ToList();

        // Whole role phrases rarely appear verbatim ("QA Engineer" vs "Quality Assurance Engineer"),
        // so the distinctive single words of each role count as keywords too.
        var roleWords = roles
            .SelectMany(role => role.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(word => word.Length >= MinRoleWordLength);

        var keywords = profile.HardSkills
            .Concat(profile.Courses)
            .Concat(roles)
            .Concat(roleWords)
            .Concat(profile.EducationField is { } field ? [field] : [])
            .ToList();

        return new ProfileTerms(keywords, profile.Cities, profile.WorkModes.Contains("remote"), profile.Avoid);
    }
}
