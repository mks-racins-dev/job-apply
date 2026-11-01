namespace JobMagnet.Web.Api;

// Mirrors the JSON schema in CvProfileExtractor; mutable so the review form can bind to it.
public class ProfileData
{
    public string Headline { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = [];

    public List<string> Skills { get; set; } = [];

    public string? Seniority { get; set; }

    public double YearsOfExperience { get; set; }

    public List<string> Languages { get; set; } = [];

    public string Location { get; set; } = string.Empty;

    public ProfilePreferences Preferences { get; set; } = new();
}
