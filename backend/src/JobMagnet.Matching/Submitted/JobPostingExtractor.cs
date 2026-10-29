using System.Text.Json;

namespace JobMagnet.Matching.Submitted;

// Turns a job posting the user pasted (often a whole LinkedIn page with navigation noise) into job fields.
public class JobPostingExtractor(IClaudeJsonClient claude)
{
    private const string SystemPrompt = """
        You extract a single job posting from text a user copied from a job site such as LinkedIn.
        The text may include navigation, ads, "people also viewed" lists and other noise: ignore it.
        Keep title, company and city as written. Salary is monthly gross in EUR; convert yearly figures
        by dividing by 12, and use null when no salary is stated or it is hourly. The description is the
        posting's own content (responsibilities, requirements, offer), cleaned of noise, at most ~3000 characters.
        If the text contains no job posting at all, set title to an empty string.
        """;

    private static readonly IReadOnlyDictionary<string, JsonElement> Schema = JsonSchema.Parse("""
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["title", "company", "city", "salaryMin", "salaryMax", "description"],
          "properties": {
            "title": { "type": "string" },
            "company": { "type": ["string", "null"] },
            "city": { "type": ["string", "null"] },
            "salaryMin": { "type": ["number", "null"] },
            "salaryMax": { "type": ["number", "null"] },
            "description": { "type": "string" }
          }
        }
        """);

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<ExtractedJob?> ExtractAsync(string postingText, CancellationToken cancellationToken)
    {
        var json = await claude.CompleteJsonAsync(SystemPrompt, $"<posting>\n{postingText}\n</posting>", Schema, cancellationToken)
            .ConfigureAwait(false);
        if (json is null)
        {
            return null;
        }

        var job = JsonSerializer.Deserialize<ExtractedJob>(json, SerializerOptions);
        return string.IsNullOrWhiteSpace(job?.Title) ? null : job;
    }
}
