using JobMagnet.Web.Components.Common;

namespace JobMagnet.Tests.Web;

public class RelativeTimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, "just now")]
    [InlineData(30, "just now")]
    [InlineData(60 * 5, "5 min ago")]
    [InlineData(60 * 59, "59 min ago")]
    [InlineData(60 * 60 * 3, "3 h ago")]
    [InlineData(60 * 60 * 30, "yesterday")]
    [InlineData(60 * 60 * 24 * 4, "4 days ago")]
    public void FormatsRecentTimes(int secondsAgo, string expected) =>
        Assert.Equal(expected, RelativeTime.Format(Now.AddSeconds(-secondsAgo), Now));

    [Fact]
    public void OlderThanAWeek_ShowsTheDate() =>
        Assert.Matches(@"^\d{2}\.\d{2}\.\d{4}$", RelativeTime.Format(Now.AddDays(-10), Now));

    [Fact]
    public void Missing_IsNever() => Assert.Equal("never", RelativeTime.Format(null, Now));
}
