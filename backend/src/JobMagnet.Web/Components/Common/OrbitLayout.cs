using JobMagnet.Web.Api;

namespace JobMagnet.Web.Components.Common;

// Places job bubbles around the user: the better the match, the closer and bigger the bubble.
public static class OrbitLayout
{
    private const double GoldenAngleDegrees = 137.508;
    private const double InnerRadius = 0.30;
    private const double OuterRadius = 0.84;
    private const double InterestedPull = 0.75;

    // Sizes are a share of the orbit's width, so the layout holds from phone to desktop.
    private const double MinBubbleSize = 9;
    private const double MaxBubbleSize = 15;
    private const int MaxScore = 100;

    // The orbit shows only the strongest matches; past this it turns into a pile. The table has the rest.
    public const int MaxBubbles = 18;

    public record Placement(double LeftPercent, double TopPercent, double SizePercent);

    // Distance comes from rank, not raw score: scores bunch up (most strong matches sit in the 80s),
    // and ranking spreads them across every ring. Size still reflects the score itself.
    public static Placement Place(MatchItem match, int rank, int count, int minScore)
    {
        var range = Math.Max(MaxScore - minScore, 1);
        var strength = Math.Clamp((double)(match.Score - minScore) / range, 0, 1);
        var nearness = count <= 1 ? 1 : 1 - ((double)rank / (count - 1));

        var radius = OuterRadius - (nearness * (OuterRadius - InnerRadius));
        if (match.Status == MatchStatus.Interested)
        {
            radius *= InterestedPull;
        }

        // The golden angle spreads any number of bubbles evenly without manual slots.
        var angle = rank * GoldenAngleDegrees * Math.PI / 180;
        var left = 50 + (Math.Cos(angle) * radius * 50);
        var top = 50 + (Math.Sin(angle) * radius * 50);
        var size = MinBubbleSize + (strength * (MaxBubbleSize - MinBubbleSize));

        return new Placement(left, top, size);
    }
}
