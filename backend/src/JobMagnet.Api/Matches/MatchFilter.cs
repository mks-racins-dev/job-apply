using JobMagnet.Data.Entities;

namespace JobMagnet.Api.Matches;

public static class MatchFilter
{
    public const string All = "all";
    public const string Favorites = "favorites";
    public const string Recent = "recent";
    public const string Dismissed = "dismissed";

    public static readonly TimeSpan RecentWindow = TimeSpan.FromHours(24);

    // Unknown values fall back to "all" so a hand-edited URL still shows something.
    public static string Normalize(string? filter) => filter?.ToLowerInvariant() switch
    {
        Favorites => Favorites,
        Recent => Recent,
        Dismissed => Dismissed,
        _ => All
    };

    // Favorites and dismissed ignore the score slider: a starred job should never vanish because of it.
    public static IQueryable<Match> Apply(IQueryable<Match> matches, string filter, int minScore, DateTimeOffset now) =>
        Normalize(filter) switch
        {
            Favorites => matches.Where(m => m.Status == MatchStatus.Interested),
            Dismissed => matches.Where(m => m.Status == MatchStatus.Dismissed),
            Recent => matches.Where(m => m.Status != MatchStatus.Dismissed && m.Score >= minScore && m.CreatedAt >= now - RecentWindow),
            _ => matches.Where(m => m.Status != MatchStatus.Dismissed && m.Score >= minScore)
        };
}
