using System.Text.Json;

namespace JobMagnet.Matching.Profiles;

// What the user tells us about themselves in the profile form; stored as the profile's JSON.
// The name is for the UI only: ToScoringJson is the one view of the profile that goes to Claude.
public class CandidateProfile
{
    public const int MaxExperience = 5;
    public const int MaxListItems = 20;
    public const int MaxTextLength = 100;
    public const decimal MaxYears = 50;

    public static readonly IReadOnlyList<string> WorkModeOptions = ["onsite", "hybrid", "remote"];
    public static readonly IReadOnlyList<string> EmploymentTypeOptions = ["full-time", "part-time", "shifts"];
    public static readonly IReadOnlyList<string> LanguageLevelOptions = ["basic", "conversational", "fluent", "native"];
    public static readonly IReadOnlyList<string> EducationLevelOptions = ["secondary", "vocational", "bachelor", "master", "doctorate"];
    public static readonly IReadOnlyList<string> DrivingLicenceOptions = ["A", "B", "BE", "C1", "C", "CE", "D", "DE"];

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public List<ExperienceEntry> Experience { get; init; } = [];

    // Where they want to go next, which may differ from where they've been (IT -> accounting).
    public List<string> InterestedRoles { get; init; } = [];

    public List<string> HardSkills { get; init; } = [];

    public List<string> SoftSkills { get; init; } = [];

    public List<LanguageSkill> Languages { get; init; } = [];

    public string? EducationLevel { get; init; }

    public string? EducationField { get; init; }

    public List<string> Courses { get; init; } = [];

    public List<string> Cities { get; init; } = [];

    public List<string> WorkModes { get; init; } = [];

    public List<string> EmploymentTypes { get; init; } = [];

    public decimal? SalaryMin { get; init; }

    public List<string> DrivingLicences { get; init; } = [];

    // Roles or fields to stay away from ("sales", "night shifts").
    public List<string> Avoid { get; init; } = [];

    public static CandidateProfile? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<CandidateProfile>(json, SerializerOptions)?.Normalize();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

    // Everything that helps scoring, nothing that identifies the person.
    public string ToScoringJson() => JsonSerializer.Serialize(new
    {
        Experience,
        InterestedRoles,
        HardSkills,
        SoftSkills,
        Languages,
        Education = EducationLevel is null && EducationField is null ? null : new { Level = EducationLevel, Field = EducationField },
        Courses,
        Cities,
        WorkModes,
        EmploymentTypes,
        SalaryMin,
        DrivingLicences,
        Avoid
    }, SerializerOptions);

    // Trims, drops blanks and duplicates, caps sizes and keeps only known options, so stored data stays clean.
    public CandidateProfile Normalize() => new()
    {
        FirstName = Text(FirstName) ?? string.Empty,
        LastName = Text(LastName) ?? string.Empty,
        Experience = (Experience ?? [])
            .Select(e => new ExperienceEntry { Role = Text(e?.Role) ?? string.Empty, Years = Math.Clamp(e?.Years ?? 0, 0, MaxYears) })
            .Where(e => e.Role.Length > 0)
            .DistinctBy(e => e.Role, StringComparer.OrdinalIgnoreCase)
            .Take(MaxExperience)
            .ToList(),
        InterestedRoles = Texts(InterestedRoles),
        HardSkills = Texts(HardSkills),
        SoftSkills = Texts(SoftSkills),
        Languages = (Languages ?? [])
            .Select(l => new LanguageSkill { Language = Text(l?.Language) ?? string.Empty, Level = Option(l?.Level, LanguageLevelOptions) ?? "basic" })
            .Where(l => l.Language.Length > 0)
            .DistinctBy(l => l.Language, StringComparer.OrdinalIgnoreCase)
            .Take(MaxListItems)
            .ToList(),
        EducationLevel = Option(EducationLevel, EducationLevelOptions),
        EducationField = Text(EducationField),
        Courses = Texts(Courses),
        Cities = Texts(Cities),
        WorkModes = Options(WorkModes, WorkModeOptions),
        EmploymentTypes = Options(EmploymentTypes, EmploymentTypeOptions),
        SalaryMin = SalaryMin is > 0 ? SalaryMin : null,
        DrivingLicences = Options(DrivingLicences, DrivingLicenceOptions),
        Avoid = Texts(Avoid)
    };

    // Returns why the profile can't be matched yet, or null when it's good to go.
    public string? Validate()
    {
        if (FirstName.Length == 0)
        {
            return "Tell us your first name.";
        }

        if (Experience.Count == 0 && InterestedRoles.Count == 0)
        {
            return "Add at least one previous role or a role you're interested in.";
        }

        return Cities.Count == 0 && !WorkModes.Contains("remote")
            ? "Add a city you'd work in, or choose remote."
            : null;
    }

    private static string? Text(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > MaxTextLength ? trimmed[..MaxTextLength] : trimmed;
    }

    private static List<string> Texts(IEnumerable<string>? values) =>
        (values ?? []).Select(Text).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxListItems).ToList();

    private static string? Option(string? value, IReadOnlyList<string> options) =>
        options.FirstOrDefault(o => string.Equals(o, value?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static List<string> Options(IEnumerable<string>? values, IReadOnlyList<string> options) =>
        (values ?? []).Select(v => Option(v, options)).OfType<string>().Distinct().ToList();
}
