using System.Text.Json;

namespace JobMagnet.Matching.Scoring;

public record ProfileTerms(IReadOnlyList<string> Keywords, IReadOnlyList<string> Cities, bool Remote)
{
    private const int MinRoleWordLength = 4;

    public static ProfileTerms FromJson(string profileJson)
    {
        using var document = JsonDocument.Parse(profileJson);
        var root = document.RootElement;

        var roles = ReadStrings(root, "roles").ToList();

        // Whole role phrases rarely appear verbatim ("QA Engineer" vs "Quality Assurance Engineer"),
        // so the distinctive single words of each role count as keywords too.
        var roleWords = roles
            .SelectMany(role => role.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(word => word.Length >= MinRoleWordLength);

        var keywords = ReadStrings(root, "skills").Concat(roles).Concat(roleWords).ToList();
        var cities = new List<string>();
        var remote = false;

        if (root.TryGetProperty("preferences", out var preferences) && preferences.ValueKind == JsonValueKind.Object)
        {
            cities.AddRange(ReadStrings(preferences, "cities"));
            remote = preferences.TryGetProperty("remote", out var remoteValue) && remoteValue.ValueKind == JsonValueKind.True;
        }

        if (root.TryGetProperty("location", out var location) && location.ValueKind == JsonValueKind.String)
        {
            cities.Add(location.GetString()!);
        }

        return new ProfileTerms(keywords, cities, remote);
    }

    private static IEnumerable<string> ReadStrings(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }
}
