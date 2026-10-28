using JobMagnet.Web.Api;
using JobMagnet.Web.Components.Shared;

namespace JobMagnet.Tests.Web;

public class OrbitLayoutTests
{
    private const int MinScore = 50;

    [Fact]
    public void HigherScore_IsCloserAndBigger()
    {
        var strong = OrbitLayout.Place(CreateMatch(95, MatchStatus.Seen), 0, MinScore);
        var weak = OrbitLayout.Place(CreateMatch(55, MatchStatus.Seen), 0, MinScore);

        Assert.True(DistanceFromCenter(strong) < DistanceFromCenter(weak));
        Assert.True(strong.SizePx > weak.SizePx);
    }

    [Fact]
    public void Interested_IsPulledCloser()
    {
        var seen = OrbitLayout.Place(CreateMatch(70, MatchStatus.Seen), 3, MinScore);
        var interested = OrbitLayout.Place(CreateMatch(70, MatchStatus.Interested), 3, MinScore);

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
            var place = OrbitLayout.Place(CreateMatch(score, MatchStatus.New), i, MinScore);

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
