using System.Globalization;
using System.Text;
using System.Text.Json;

namespace JobMagnet.Scrapers.Parsing;

// Parses the CKAN datastore_search response for the NVA (State Employment Agency) open-data
// vacancy dataset: https://data.gov.lv/dati/dataset/vakances (CC0, refreshed daily).
public static class NvaParser
{
    public const string SourceName = "nva.gov.lv";
    public const string ApiBaseUrl = "https://data.gov.lv/dati/api/3/action";
    public const string DatasetId = "vakances";

    public record Page(IReadOnlyList<ScrapedJob> Jobs, int Total);

    // The dataset publishes a new CSV resource daily, so the resource id is looked up from package_show.
    public static string? ParseResourceId(string packageJson)
    {
        using var doc = JsonDocument.Parse(packageJson);
        if (!doc.RootElement.TryGetProperty("result", out var result) ||
            !result.TryGetProperty("resources", out var resources))
        {
            return null;
        }

        foreach (var resource in resources.EnumerateArray())
        {
            if (resource.TryGetProperty("datastore_active", out var active) &&
                active.ValueKind == JsonValueKind.True &&
                resource.TryGetProperty("id", out var id))
            {
                return id.GetString();
            }
        }

        return null;
    }

    public static Page Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("result", out var result) ||
            !result.TryGetProperty("records", out var records))
        {
            return new Page([], 0);
        }

        var total = result.TryGetProperty("total", out var t) && t.TryGetInt32(out var n) ? n : 0;
        var jobs = new List<ScrapedJob>();

        foreach (var record in records.EnumerateArray())
        {
            var id = GetString(record, "Vakances_Nr");
            var title = GetString(record, "Vakances_nosaukums");
            var url = GetString(record, "Vakances_paplasinats_apraksts");
            if (id is null || title is null)
            {
                continue;
            }

            var salaryMin = GetDecimal(record, "Alga_no");
            var salaryMax = GetDecimal(record, "Alga_lidz") ?? salaryMin;
            var place = GetString(record, "Vieta");

            jobs.Add(new ScrapedJob(
                Source: SourceName,
                ExternalId: id,
                Url: url ?? "https://cvvp.nva.gov.lv/#/pub/vakances",
                Title: title,
                Company: null, // the dataset only carries the employer's registration number
                City: ExtractCity(place),
                SalaryMin: salaryMin > 0 ? salaryMin : null,
                SalaryMax: salaryMax > 0 ? salaryMax : null,
                Description: BuildDescription(record, place),
                PostedAt: GetDate(record, "Aktualizacijas_datums"),
                Deadline: GetDate(record, "Pieteiksanas_termins")));
        }

        return new Page(jobs, total);
    }

    // "Riekstu iela 2, Lielvārde, Ogres nov." -> "Lielvārde"; "Dārzciema iela 86, Rīga" -> "Rīga".
    public static string? ExtractCity(string? place)
    {
        if (string.IsNullOrWhiteSpace(place))
        {
            return null;
        }

        var parts = place.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.EndsWith("nov.", StringComparison.OrdinalIgnoreCase) &&
                        !p.EndsWith("pag.", StringComparison.OrdinalIgnoreCase) &&
                        !p.StartsWith("LV-", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return parts.Count > 0 ? parts[^1] : null;
    }

    private static string BuildDescription(JsonElement record, string? place)
    {
        var sb = new StringBuilder();
        Append(sb, "Kategorija", GetString(record, "Vakances_kategorija"));
        Append(sb, "Slodze", GetString(record, "Slodzes_tips"));
        Append(sb, "Darba laiks", GetString(record, "Darba_laika_veids"));
        var hours = GetDecimal(record, "Darba_stundas_nedela");
        Append(sb, "Stundas nedēļā", hours?.ToString("0.#", CultureInfo.InvariantCulture));
        Append(sb, "Vieta", place);
        Append(sb, "Pieteikšanās termiņš", GetDate(record, "Pieteiksanas_termins")?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        var regNo = GetString(record, "Iestades_registracijas_numurs");
        Append(sb, "Darba devēja reģ. nr.", regNo);
        return sb.ToString().TrimEnd();
    }

    private static void Append(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sb.Append(label).Append(": ").AppendLine(value);
        }
    }

    private static string? GetString(JsonElement record, string name)
    {
        if (!record.TryGetProperty(name, out var value))
        {
            return null;
        }

        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static decimal? GetDecimal(JsonElement record, string name) =>
        record.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var d)
            ? d
            : null;

    private static DateTimeOffset? GetDate(JsonElement record, string name) =>
        GetString(record, name) is { } text &&
        DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date)
            ? new DateTimeOffset(date, TimeSpan.Zero)
            : null;
}
