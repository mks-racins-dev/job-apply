using System.Text;
using System.Text.Json;

using JobMagnet.Data.Entities;
using JobMagnet.Matching.Profiles;

namespace JobMagnet.Matching.Scoring;

public class JobScorer(IClaudeJsonClient claude)
{
    private const int MaxScore = 100;
    private const int DescriptionMaxLength = 1500;

    private const string SystemPrompt = """
        You score how well Latvian job listings fit a candidate profile, from 0 (no fit) to 100 (ideal fit).
        Listings may be in Latvian, English or Russian. The candidate filled in the profile themselves:
        - interestedRoles are where they want to go next and count as much as past experience; for a career
          change, credit transferable hard skills, courses and education instead of requiring years in the role.
        - Weigh role and skills most, then language levels against the listing's requirements, then location
          (cities, or remote in workModes), work mode, employment type and salary (salaryMin is monthly gross EUR).
        - A required driving licence category the candidate lacks is a strong minus.
        - Jobs matching anything in avoid score below 20.
        Listings are often short, so judge from what is there and don't reward missing information. The reason
        is one short sentence addressed to the candidate explaining why this job wants them (or why it doesn't).
        Return one result for every job id you were given.
        """;

    private static readonly IReadOnlyDictionary<string, JsonElement> Schema = JsonSchema.Parse("""
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["results"],
          "properties": {
            "results": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["jobId", "score", "reason", "matchedSkills", "missingSkills"],
                "properties": {
                  "jobId": { "type": "string" },
                  "score": { "type": "integer" },
                  "reason": { "type": "string" },
                  "matchedSkills": { "type": "array", "items": { "type": "string" } },
                  "missingSkills": { "type": "array", "items": { "type": "string" } }
                }
              }
            }
          }
        }
        """);

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private record ScoringResponse(List<ScoringResult> Results);

    private record ScoringResult(string JobId, int Score, string Reason, List<string> MatchedSkills, List<string> MissingSkills);

    // Takes the profile, not its JSON, so only the anonymous scoring view can ever reach Claude.
    public async Task<IReadOnlyList<JobScore>> ScoreAsync(CandidateProfile profile, IReadOnlyList<Job> jobs, CancellationToken cancellationToken)
    {
        var json = await claude.CompleteJsonAsync(SystemPrompt, BuildPrompt(profile.ToScoringJson(), jobs), Schema, cancellationToken)
            .ConfigureAwait(false);

        return json is null ? [] : ParseResponse(json, jobs);
    }

    public static IReadOnlyList<JobScore> ParseResponse(string json, IReadOnlyList<Job> jobs)
    {
        var response = JsonSerializer.Deserialize<ScoringResponse>(json, SerializerOptions);
        var requestedIds = jobs.Select(j => j.Id).ToHashSet();

        // Drop ids Claude didn't get from us; clamp because the schema can't enforce a range.
        return (response?.Results ?? [])
            .Where(r => Guid.TryParse(r.JobId, out var id) && requestedIds.Contains(id))
            .DistinctBy(r => r.JobId)
            .Select(r => new JobScore(Guid.Parse(r.JobId), Math.Clamp(r.Score, 0, MaxScore), r.Reason, r.MatchedSkills, r.MissingSkills))
            .ToList();
    }

    private static string BuildPrompt(string profileJson, IReadOnlyList<Job> jobs)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"<profile>\n{profileJson}\n</profile>");
        builder.AppendLine("<jobs>");
        foreach (var job in jobs)
        {
            var description = job.Description.Length > DescriptionMaxLength ? job.Description[..DescriptionMaxLength] : job.Description;
            builder.AppendLine($"""
                <job id="{job.Id}">
                Title: {job.Title}
                Company: {job.Company ?? "unknown"}
                City: {job.City ?? "unknown"}
                Salary: {FormatSalary(job)}
                Description: {description}
                </job>
                """);
        }

        builder.AppendLine("</jobs>");
        return builder.ToString();
    }

    private static string FormatSalary(Job job) => (job.SalaryMin, job.SalaryMax) switch
    {
        (null, null) => "not stated",
        ({ } min, { } max) when min != max => $"{min}-{max} EUR/month",
        _ => $"{job.SalaryMin ?? job.SalaryMax} EUR/month"
    };
}
