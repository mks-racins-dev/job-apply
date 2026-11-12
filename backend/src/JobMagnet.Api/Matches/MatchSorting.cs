using JobMagnet.Data.Entities;

namespace JobMagnet.Api.Matches;

public static class MatchSorting
{
    public const string Ascending = "asc";

    // Unknown keys fall back to score so a hand-edited URL still returns something sensible.
    public static IQueryable<Match> Apply(IQueryable<Match> matches, string? sort, string? dir)
    {
        var descending = !string.Equals(dir, Ascending, StringComparison.OrdinalIgnoreCase);

        var ordered = sort?.ToLowerInvariant() switch
        {
            "title" => Order(matches, m => m.Job!.Title, descending),
            "company" => Order(matches, m => m.Job!.Company ?? string.Empty, descending),
            "city" => Order(matches, m => m.Job!.City ?? string.Empty, descending),
            // Jobs without a salary always go last, whichever direction is chosen.
            "salary" => Order(
                matches.OrderBy(m => m.Job!.SalaryMax == null && m.Job.SalaryMin == null),
                m => m.Job!.SalaryMax ?? m.Job.SalaryMin,
                descending),
            "source" => Order(matches, m => m.Job!.Source, descending),
            // Jobs without a deadline always go last.
            "deadline" => Order(matches.OrderBy(m => m.Job!.Deadline == null), m => m.Job!.Deadline, descending),
            "added" => Order(matches, m => m.CreatedAt, descending),
            _ => Order(matches, m => m.Score, descending)
        };

        // Ties keep the best match first, then the id makes paging stable between requests.
        return ordered.ThenByDescending(m => m.Score).ThenBy(m => m.Id);
    }

    private static IOrderedQueryable<Match> Order<TKey>(
        IOrderedQueryable<Match> matches, System.Linq.Expressions.Expression<Func<Match, TKey>> key, bool descending) =>
        descending ? matches.ThenByDescending(key) : matches.ThenBy(key);

    private static IOrderedQueryable<Match> Order<TKey>(
        IQueryable<Match> matches, System.Linq.Expressions.Expression<Func<Match, TKey>> key, bool descending) =>
        descending ? matches.OrderByDescending(key) : matches.OrderBy(key);
}
