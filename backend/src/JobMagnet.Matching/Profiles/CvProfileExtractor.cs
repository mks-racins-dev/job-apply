namespace JobMagnet.Matching.Profiles;

public class CvProfileExtractor(IClaudeJsonClient claude)
{
    private const string SystemPrompt = """
        You turn a CV into a structured candidate profile for matching against Latvian job listings.
        CVs may be written in Latvian, English or Russian; write the profile in English but keep
        proper names (companies, schools, technologies) as written. Only include facts the CV supports.
        For seniority use one of: intern, junior, mid, senior, lead, or null when the CV does not show it.
        Languages use ISO 639-1 codes.
        """;

    private static readonly IReadOnlyDictionary<string, System.Text.Json.JsonElement> Schema = JsonSchema.Parse("""
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["headline", "roles", "skills", "seniority", "yearsOfExperience", "languages", "location", "preferences"],
          "properties": {
            "headline": { "type": "string" },
            "roles": { "type": "array", "items": { "type": "string" } },
            "skills": { "type": "array", "items": { "type": "string" } },
            "seniority": { "type": ["string", "null"], "enum": ["intern", "junior", "mid", "senior", "lead", null] },
            "yearsOfExperience": { "type": "number" },
            "languages": { "type": "array", "items": { "type": "string" } },
            "location": { "type": "string" },
            "preferences": {
              "type": "object",
              "additionalProperties": false,
              "required": ["remote", "cities", "salaryMin"],
              "properties": {
                "remote": { "type": "boolean" },
                "cities": { "type": "array", "items": { "type": "string" } },
                "salaryMin": { "type": ["number", "null"] }
              }
            }
          }
        }
        """);

    public Task<string?> ExtractAsync(string cvText, CancellationToken cancellationToken) =>
        claude.CompleteJsonAsync(SystemPrompt, $"<cv>\n{cvText}\n</cv>", Schema, cancellationToken);
}
