using JobMagnet.Web.Api;

namespace JobMagnet.Web.Components.Shared;

// Places job bubbles around the user: the better the match, the closer and bigger the bubble.
public static class OrbitLayout
{
    private const double GoldenAngleDegrees = 137.508;
    private const double InnerRadius = 0.30;
    private const double OuterRadius = 0.92;
    private const double InterestedPull = 0.75;
    private const double MinBubbleSize = 64;
    private const double MaxBubbleSize = 112;
    private const int MaxScore = 100;

    public record Placement(double LeftPercent, double TopPercent, double SizePx);

    public static Placement Place(MatchItem match, int index, int minScore)
    {
        var range = Math.Max(MaxScore - minScore, 1);
        var closeness = Math.Clamp((double)(match.Score - minScore) / range, 0, 1);

        var radius = OuterRadius - (closeness * (OuterRadius - InnerRadius));
        if (match.Status == MatchStatus.Interested)
        {
            radius *= InterestedPull;
        }

        // The golden angle spreads any number of bubbles evenly without manual slots.
        var angle = index * GoldenAngleDegrees * Math.PI / 180;
        var left = 50 + (Math.Cos(angle) * radius * 50);
        var top = 50 + (Math.Sin(angle) * radius * 50);
        var size = MinBubbleSize + (closeness * (MaxBubbleSize - MinBubbleSize));

        return new Placement(left, top, size);
    }
}
