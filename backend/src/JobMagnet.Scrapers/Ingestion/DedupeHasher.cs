using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace JobMagnet.Scrapers.Ingestion;

public static class DedupeHasher
{
    // Title + company + city identifies the same vacancy cross-posted on several sites;
    // diacritics and case are stripped because sites differ in how they spell them.
    public static string Compute(ScrapedJob job)
    {
        var key = string.Join('|', Normalize(job.Title), Normalize(job.Company), Normalize(job.City));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
            else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && builder.Length > 0 && builder[^1] != ' ')
            {
                builder.Append(' ');
            }
        }

        return builder.ToString().Trim();
    }
}
