using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JobMagnet.Scrapers.Parsing;

public static partial class CvLvParser
{
    public const string SourceName = "cv.lv";
    public const string BaseUrl = "https://www.cv.lv";

    public static IReadOnlyList<ScrapedJob> Parse(string html)
    {
        var match = NextDataRegex().Match(html);
        if (!match.Success)
        {
            return [];
        }

        using var data = JsonDocument.Parse(match.Groups[1].Value);
        if (!TryGetVacancies(data.RootElement, out var vacancies))
        {
            return [];
        }

        return vacancies.EnumerateArray().Select(ToJob).ToList();
    }

    private static bool TryGetVacancies(JsonElement root, out JsonElement vacancies)
    {
        vacancies = default;
        return root.TryGetProperty("props", out var props)
            && props.TryGetProperty("pageProps", out var pageProps)
            && pageProps.TryGetProperty("searchResults", out var results)
            && results.TryGetProperty("vacancies", out vacancies)
            && vacancies.ValueKind == JsonValueKind.Array;
    }

    private static ScrapedJob ToJob(JsonElement vacancy)
    {
        var id = vacancy.GetProperty("id").GetInt64().ToString(CultureInfo.InvariantCulture);
        var isHourly = GetBool(vacancy, "hourlySalary");

        return new ScrapedJob(
            Source: SourceName,
            ExternalId: id,
            Url: $"{BaseUrl}/lv/vacancy/{id}",
            Title: GetString(vacancy, "positionTitle") ?? string.Empty,
            Company: GetString(vacancy, "employerName"),
            // cv.lv only exposes numeric town ids here; resolving them needs its lookup data.
            City: null,
            SalaryMin: isHourly ? null : GetDecimal(vacancy, "salaryFrom"),
            SalaryMax: isHourly ? null : GetDecimal(vacancy, "salaryTo"),
            Description: GetString(vacancy, "positionContent") ?? string.Empty,
            PostedAt: DateTimeOffset.TryParse(GetString(vacancy, "publishDate"), out var posted) ? posted : null);
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static decimal? GetDecimal(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDecimal() : null;

    private static bool GetBool(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    [GeneratedRegex("<script id=\"__NEXT_DATA__\"[^>]*>(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex NextDataRegex();
}
