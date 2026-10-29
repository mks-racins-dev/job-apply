using System.Security.Cryptography;
using System.Text;

using JobMagnet.Data;
using JobMagnet.Data.Entities;

namespace JobMagnet.Matching.Submitted;

public static class SubmittedJobFactory
{
    public const string LinkedInSource = "linkedin.com";
    public const string ManualSource = "manual";

    private const string LinkedInHost = "linkedin.com";

    // Only http(s) links are kept: the URL ends up in an href, so javascript: and friends must never get through.
    public static bool TryNormalizeUrl(string? url, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        normalized = uri.GetLeftPart(UriPartial.Path);
        return true;
    }

    public static string DetectSource(string normalizedUrl) =>
        Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri)
            && (uri.Host.Equals(LinkedInHost, StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith($".{LinkedInHost}", StringComparison.OrdinalIgnoreCase))
            ? LinkedInSource
            : ManualSource;

    public static Job Create(Guid userId, string normalizedUrl, string postingText, ExtractedJob extracted, DateTimeOffset now)
    {
        return new Job
        {
            Source = DetectSource(normalizedUrl),
            ExternalId = ComputeExternalId(userId, normalizedUrl, postingText),
            Url = normalizedUrl,
            Title = extracted.Title.Trim(),
            Company = extracted.Company,
            City = extracted.City,
            SalaryMin = extracted.SalaryMin,
            SalaryMax = extracted.SalaryMax,
            Description = extracted.Description,
            PostedAt = now,
            FirstSeenAt = now,
            LastSeenAt = now,
            SubmittedByUserId = userId,
            DedupeHash = Hash(string.Join('|', TextNormalizer.Normalize(extracted.Title), TextNormalizer.Normalize(extracted.Company), TextNormalizer.Normalize(extracted.City)))
        };
    }

    // Pasting the same posting twice should find the existing job, so the id is derived from user + link (or text).
    public static string ComputeExternalId(Guid userId, string normalizedUrl, string postingText) =>
        Hash($"{userId}|{(normalizedUrl.Length > 0 ? normalizedUrl : TextNormalizer.Normalize(postingText))}");

    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
