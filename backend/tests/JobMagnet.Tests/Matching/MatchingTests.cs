using JobMagnet.Data.Entities;
using JobMagnet.Matching.Scoring;

namespace JobMagnet.Tests.Matching;

public class MatchingTests
{
    private const string ProfileJson = """
        {
          "headline": "Backend developer",
          "roles": ["Backend developer"],
          "skills": ["C#", ".NET", "PostgreSQL", "Docker"],
          "seniority": "senior",
          "yearsOfExperience": 7,
          "languages": ["lv", "en"],
          "location": "Rīga",
          "preferences": { "remote": true, "cities": ["Rīga"], "salaryMin": 3000 }
        }
        """;

    [Fact]
    public void ProfileTerms_ReadsSkillsRolesAndCities()
    {
        var terms = ProfileTerms.FromJson(ProfileJson);

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

        var candidates = CandidatePrefilter.SelectCandidates(ProfileTerms.FromJson(ProfileJson), [weak, unrelated, strong], 10);

        Assert.Equal([strong, weak], candidates);
    }

    [Fact]
    public void Prefilter_RespectsMaxCandidates()
    {
        var jobs = Enumerable.Range(0, 5).Select(i => CreateJob($"PostgreSQL DBA {i}", string.Empty, null)).ToList();

        Assert.Equal(2, CandidatePrefilter.SelectCandidates(ProfileTerms.FromJson(ProfileJson), jobs, 2).Count);
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
