using JobMagnet.Api.Matches;
using JobMagnet.Data.Entities;

namespace JobMagnet.Tests.Api;

public class MatchFilterTests
{
    private const int MinScore = 50;

    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private static readonly Match FreshStrong = CreateMatch(80, MatchStatus.New, hoursAgo: 2);
    private static readonly Match OldStrong = CreateMatch(75, MatchStatus.Seen, hoursAgo: 48);
    private static readonly Match FreshWeak = CreateMatch(30, MatchStatus.New, hoursAgo: 1);
    private static readonly Match LowFavorite = CreateMatch(20, MatchStatus.Interested, hoursAgo: 72);
    private static readonly Match Dismissed = CreateMatch(90, MatchStatus.Dismissed, hoursAgo: 3);

    private static readonly Match[] All = [FreshStrong, OldStrong, FreshWeak, LowFavorite, Dismissed];

    [Fact]
    public void All_HidesDismissedAndLowScores() =>
        Assert.Equal([FreshStrong, OldStrong], Filter(MatchFilter.All));

    [Fact]
    public void Favorites_IgnoreTheScoreSlider() =>
        Assert.Equal([LowFavorite], Filter(MatchFilter.Favorites));

    [Fact]
    public void Recent_IsLast24HoursAboveTheMinimum() =>
        Assert.Equal([FreshStrong], Filter(MatchFilter.Recent));

    [Fact]
    public void Dismissed_ShowsOnlyDismissed() =>
        Assert.Equal([Dismissed], Filter(MatchFilter.Dismissed));

    [Theory]
    [InlineData(null, MatchFilter.All)]
    [InlineData("FAVORITES", MatchFilter.Favorites)]
    [InlineData("nonsense", MatchFilter.All)]
    public void Normalize_FallsBackToAll(string? input, string expected) =>
        Assert.Equal(expected, MatchFilter.Normalize(input));

    private static List<Match> Filter(string filter) =>
        MatchFilter.Apply(All.AsQueryable(), filter, MinScore, Now).ToList();

    private static Match CreateMatch(int score, MatchStatus status, int hoursAgo) => new()
    {
        Id = Guid.NewGuid(),
        Score = score,
        Status = status,
        Reason = "reason",
        CreatedAt = Now.AddHours(-hoursAgo)
    };
}
