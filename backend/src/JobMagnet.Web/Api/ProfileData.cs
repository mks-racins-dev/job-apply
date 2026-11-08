namespace JobMagnet.Web.Api;

// Mirrors CandidateProfile in JobMagnet.Matching; mutable so the profile form can bind to it.
public class ProfileData
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public List<ExperienceEntry> Experience { get; set; } = [];

    public List<string> InterestedRoles { get; set; } = [];

    public List<string> HardSkills { get; set; } = [];

    public List<string> SoftSkills { get; set; } = [];

    public List<LanguageSkill> Languages { get; set; } = [];

    public string? EducationLevel { get; set; }

    public string? EducationField { get; set; }

    public List<string> Courses { get; set; } = [];

    public List<string> Cities { get; set; } = [];

    public List<string> WorkModes { get; set; } = [];

    public List<string> EmploymentTypes { get; set; } = [];

    public decimal? SalaryMin { get; set; }

    public List<string> DrivingLicences { get; set; } = [];

    public List<string> Avoid { get; set; } = [];
}
