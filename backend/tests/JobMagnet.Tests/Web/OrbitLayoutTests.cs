using JobMagnet.Web.Api;
using JobMagnet.Web.Components.Common;

namespace JobMagnet.Tests.Web;

public class OrbitLayoutTests
{
    private const int MinScore = 50;
    private const int Count = 10;

    [Fact]
    public void HigherScore_IsCloserAndBigger()
    {
        var strong = OrbitLayout.Place(CreateMatch(95, MatchStatus.Seen), 0, Count, MinScore);
        var weak = OrbitLayout.Place(CreateMatch(55, MatchStatus.Seen), Count - 1, Count, MinScore);

        Assert.True(DistanceFromCenter(strong) < DistanceFromCenter(weak));
        Assert.True(strong.SizePercent > weak.SizePercent);
    }

    [Fact]
    public void Interested_IsPulledCloser()
    {
        var seen = OrbitLayout.Place(CreateMatch(70, MatchStatus.Seen), 3, Count, MinScore);
        var interested = OrbitLayout.Place(CreateMatch(70, MatchStatus.Interested), 3, Count, MinScore);

        Assert.True(DistanceFromCenter(interested) < DistanceFromCenter(seen));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Bubbles_StayInsideTheOrbit(int score)
    {
        for (var i = 0; i < 30; i++)
        {
            var place = OrbitLayout.Place(CreateMatch(score, MatchStatus.New), i, 30, MinScore);

            Assert.InRange(place.LeftPercent, 0, 100);
            Assert.InRange(place.TopPercent, 0, 100);
        }
    }

    private static double DistanceFromCenter(OrbitLayout.Placement place) =>
        Math.Sqrt(Math.Pow(place.LeftPercent - 50, 2) + Math.Pow(place.TopPercent - 50, 2));

    private static MatchItem CreateMatch(int score, MatchStatus status) => new(
        Guid.NewGuid(), score, "reason", [], [], status, DateTimeOffset.UtcNow,
        new JobItem(Guid.NewGuid(), "Developer", null, null, null, null, "test", "https://example.com"));
}
