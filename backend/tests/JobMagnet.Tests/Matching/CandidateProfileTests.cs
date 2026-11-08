using JobMagnet.Matching.Profiles;

namespace JobMagnet.Tests.Matching;

public class CandidateProfileTests
{
    private static readonly CandidateProfile Valid = new()
    {
        FirstName = "Anna",
        LastName = "Kalniņa",
        Experience = [new ExperienceEntry { Role = "Cashier", Years = 2 }],
        Cities = ["Rīga"]
    };

    [Fact]
    public void ScoringJson_NeverContainsTheName()
    {
        var json = Valid.ToScoringJson();

        Assert.DoesNotContain("Anna", json);
        Assert.DoesNotContain("Kalniņa", json);
        Assert.DoesNotContain("firstName", json);
        Assert.Contains("Cashier", json);
    }

    [Fact]
    public void Normalize_TrimsDedupesAndDropsUnknownOptions()
    {
        var profile = new CandidateProfile
        {
            FirstName = "  Anna ",
            Experience = [new ExperienceEntry { Role = " Cashier " }, new ExperienceEntry { Role = "cashier" }, new ExperienceEntry { Role = " " }],
            HardSkills = ["Excel", "excel", ""],
            Languages = [new LanguageSkill { Language = "Latvian", Level = "NATIVE" }, new LanguageSkill { Language = "English", Level = "guru" }],
            WorkModes = ["remote", "on the moon"],
            DrivingLicences = ["b", "Z"],
            SalaryMin = -5
        }.Normalize();

        Assert.Equal("Anna", profile.FirstName);
        Assert.Equal(["Cashier"], profile.Experience.Select(e => e.Role));
        Assert.Equal(["Excel"], profile.HardSkills);
        Assert.Equal(["native", "basic"], profile.Languages.Select(l => l.Level));
        Assert.Equal(["remote"], profile.WorkModes);
        Assert.Equal(["B"], profile.DrivingLicences);
        Assert.Null(profile.SalaryMin);
    }

    [Fact]
    public void Normalize_KeepsAtMostFiveRoles()
    {
        var profile = new CandidateProfile
        {
            Experience = Enumerable.Range(1, 8).Select(i => new ExperienceEntry { Role = $"Role {i}" }).ToList()
        }.Normalize();

        Assert.Equal(CandidateProfile.MaxExperience, profile.Experience.Count);
    }

    [Fact]
    public void Validate_AcceptsACompleteProfile() => Assert.Null(Valid.Validate());

    [Fact]
    public void Validate_NeedsARole()
    {
        var profile = new CandidateProfile { FirstName = "Anna", Cities = ["Rīga"] };

        Assert.NotNull(profile.Validate());
    }

    [Fact]
    public void Validate_AcceptsRemoteInsteadOfACity()
    {
        var profile = new CandidateProfile { FirstName = "Anna", InterestedRoles = ["Bookkeeper"], WorkModes = ["remote"] };

        Assert.Null(profile.Validate());
    }

    [Fact]
    public void FromJson_RoundTripsAndRejectsGarbage()
    {
        Assert.Equal("Cashier", CandidateProfile.FromJson(Valid.ToJson())!.Experience[0].Role);
        Assert.Null(CandidateProfile.FromJson("not json"));
    }
}
