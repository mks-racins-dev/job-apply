using JobMagnet.Data.Entities;
using JobMagnet.Matching.Profiles;
using JobMagnet.Matching.Scoring;

namespace JobMagnet.Tests.Matching;

public class MatchingTests
{
    private static readonly CandidateProfile Profile = new()
    {
        FirstName = "Jānis",
        Experience = [new ExperienceEntry { Role = "Backend developer", Years = 7 }],
        HardSkills = ["C#", ".NET", "PostgreSQL", "Docker"],
        Cities = ["Rīga"],
        WorkModes = ["remote"],
        SalaryMin = 3000
    };

    [Fact]
    public void ProfileTerms_ReadsSkillsRolesAndCities()
    {
        var terms = ProfileTerms.FromProfile(Profile);

        Assert.Contains("PostgreSQL", terms.Keywords);
        Assert.Contains("Backend developer", terms.Keywords);
        Assert.Contains("Rīga", terms.Cities);
        Assert.True(terms.Remote);
    }

    [Fact]
    public void Prefilter_RanksByOverlapAndDropsUnrelated()
    {
        var strong = CreateJob("Senior .NET backend developer", "PostgreSQL, Docker", "Riga");
        var weak = CreateJob("Docker administrator", "Linux servers", "Jelgava");
        var unrelated = CreateJob("Sētnieks", "Teritorijas uzkopšana", "Rīga");

        var candidates = CandidatePrefilter.SelectCandidates(ProfileTerms.FromProfile(Profile), [weak, unrelated, strong], 10);

        Assert.Equal([strong, weak], candidates);
    }

    [Fact]
    public void Prefilter_MatchesSingleRoleWords()
    {
        var terms = ProfileTerms.FromProfile(new CandidateProfile { Experience = [new ExperienceEntry { Role = "QA Engineer" }] });
        var job = CreateJob("Quality Assurance Engineer", string.Empty, null);

        Assert.Single(CandidatePrefilter.SelectCandidates(terms, [job], 10));
    }

    [Fact]
    public void Prefilter_RespectsMaxCandidates()
    {
        var jobs = Enumerable.Range(0, 5).Select(i => CreateJob($"PostgreSQL DBA {i}", string.Empty, null)).ToList();

        Assert.Equal(2, CandidatePrefilter.SelectCandidates(ProfileTerms.FromProfile(Profile), jobs, 2).Count);
    }

    [Fact]
    public void Prefilter_IncludesInterestedRolesOutsideExperience()
    {
        var careerChange = new CandidateProfile
        {
            Experience = [new ExperienceEntry { Role = "Software developer", Years = 5 }],
            InterestedRoles = ["Accountant"]
        };
        var job = CreateJob("Accountant", "Grāmatvedība", "Rīga");

        Assert.Single(CandidatePrefilter.SelectCandidates(ProfileTerms.FromProfile(careerChange), [job], 10));
    }

    [Fact]
    public void Prefilter_DropsJobsWhoseTitleIsAvoided()
    {
        var profile = new CandidateProfile { HardSkills = ["Excel"], Avoid = ["Sales"] };
        var sales = CreateJob("Sales manager", "Excel reports", null);
        var analyst = CreateJob("Data analyst", "Excel reports for the sales team", null);

        Assert.Equal([analyst], CandidatePrefilter.SelectCandidates(ProfileTerms.FromProfile(profile), [sales, analyst], 10));
    }

    [Fact]
    public void ParseResponse_ClampsScoresAndIgnoresUnknownIds()
    {
        var job = CreateJob("Backend developer", string.Empty, "Rīga");
        var json = $$"""
            {"results": [
              {"jobId": "{{job.Id}}", "score": 140, "reason": "Your .NET depth fits.", "matchedSkills": [".NET"], "missingSkills": []},
              {"jobId": "{{Guid.NewGuid()}}", "score": 90, "reason": "x", "matchedSkills": [], "missingSkills": []}
            ]}
            """;

        var score = Assert.Single(JobScorer.ParseResponse(json, [job]));

        Assert.Equal(job.Id, score.JobId);
        Assert.Equal(100, score.Score);
        Assert.Equal([".NET"], score.MatchedSkills);
    }

    private static Job CreateJob(string title, string description, string? city) => new()
    {
        Id = Guid.NewGuid(),
        Source = "test",
        ExternalId = Guid.NewGuid().ToString(),
        Url = "https://example.com",
        Title = title,
        Description = description,
        City = city,
        DedupeHash = "hash",
        LastSeenAt = DateTimeOffset.UtcNow
    };
}
