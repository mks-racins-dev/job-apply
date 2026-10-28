using System.Security.Cryptography;
using System.Text;

using JobMagnet.Data;

namespace JobMagnet.Scrapers.Ingestion;

public static class DedupeHasher
{
    // Title + company + city identifies the same vacancy cross-posted on several sites.
    public static string Compute(ScrapedJob job)
    {
        var key = string.Join(
            '|',
            TextNormalizer.Normalize(job.Title),
            TextNormalizer.Normalize(job.Company),
            TextNormalizer.Normalize(job.City));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }
}
