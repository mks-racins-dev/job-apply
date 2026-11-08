using JobMagnet.Data;
using JobMagnet.Data.Entities;

namespace JobMagnet.Matching.Scoring;

public static class CandidatePrefilter
{
    private const int CityMatchBonus = 2;

    // Cheap keyword overlap so only the most plausible jobs reach Claude.
    public static IReadOnlyList<Job> SelectCandidates(ProfileTerms terms, IEnumerable<Job> jobs, int maxCandidates)
    {
        var keywords = terms.Keywords.Select(Normalize).Where(k => k.Length > 1).Distinct().ToList();
        var cities = terms.Cities.Select(Normalize).Where(c => c.Length > 0).ToHashSet();
        var avoid = terms.Avoid.Select(Normalize).Where(a => a.Length > 1).Distinct().ToList();

        return jobs
            .Where(job => !IsAvoided(job, avoid))
            .Select(job => (Job: job, Score: Score(job, keywords, cities)))
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Job.PostedAt ?? candidate.Job.LastSeenAt)
            .Take(maxCandidates)
            .Select(candidate => candidate.Job)
            .ToList();
    }

    private static int Score(Job job, IReadOnlyList<string> keywords, HashSet<string> cities)
    {
        var text = $" {Normalize($"{job.Title} {job.Description}")} ";
        var score = keywords.Count(keyword => text.Contains($" {keyword} ", StringComparison.Ordinal));

        if (score > 0 && job.City is not null && cities.Contains(Normalize(job.City)))
        {
            score += CityMatchBonus;
        }

        return score;
    }

    // Only the title: a description mentioning "sales" doesn't make a developer job a sales job.
    private static bool IsAvoided(Job job, IReadOnlyList<string> avoid)
    {
        var title = $" {Normalize(job.Title)} ";
        return avoid.Any(term => title.Contains($" {term} ", StringComparison.Ordinal));
    }

    private static string Normalize(string value) => TextNormalizer.Normalize(value);
}
