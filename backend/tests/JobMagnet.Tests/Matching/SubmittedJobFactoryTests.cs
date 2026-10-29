using JobMagnet.Matching.Submitted;

namespace JobMagnet.Tests.Matching;

public class SubmittedJobFactoryTests
{
    private static readonly ExtractedJob Extracted = new("Backend Developer", "Acme", "Rīga", 3000, 4000, "Build APIs.");

    [Theory]
    [InlineData("https://www.linkedin.com/jobs/view/4012345678/?refId=abc&trk=x", true, "https://www.linkedin.com/jobs/view/4012345678/")]
    [InlineData("  http://example.com/job  ", true, "http://example.com/job")]
    [InlineData("", true, "")]
    [InlineData(null, true, "")]
    [InlineData("javascript:alert(1)", false, "")]
    [InlineData("ftp://example.com/job", false, "")]
    [InlineData("not a url", false, "")]
    public void TryNormalizeUrl_KeepsOnlyHttpLinksWithoutTracking(string? url, bool expectedValid, string expected)
    {
        var valid = SubmittedJobFactory.TryNormalizeUrl(url, out var normalized);

        Assert.Equal(expectedValid, valid);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("https://www.linkedin.com/jobs/view/1/", SubmittedJobFactory.LinkedInSource)]
    [InlineData("https://lv.linkedin.com/jobs/view/1/", SubmittedJobFactory.LinkedInSource)]
    [InlineData("https://notlinkedin.com/jobs/1", SubmittedJobFactory.ManualSource)]
    [InlineData("", SubmittedJobFactory.ManualSource)]
    public void DetectSource_RecognisesLinkedIn(string url, string expected) =>
        Assert.Equal(expected, SubmittedJobFactory.DetectSource(url));

    [Fact]
    public void ExternalId_IsStablePerUserAndLink()
    {
        var user = Guid.NewGuid();
        const string Url = "https://www.linkedin.com/jobs/view/1/";

        Assert.Equal(
            SubmittedJobFactory.ComputeExternalId(user, Url, "first paste"),
            SubmittedJobFactory.ComputeExternalId(user, Url, "second paste with other noise"));
        Assert.NotEqual(
            SubmittedJobFactory.ComputeExternalId(user, Url, "text"),
            SubmittedJobFactory.ComputeExternalId(Guid.NewGuid(), Url, "text"));
    }

    [Fact]
    public void ExternalId_WithoutLink_UsesNormalizedText()
    {
        var user = Guid.NewGuid();

        Assert.Equal(
            SubmittedJobFactory.ComputeExternalId(user, string.Empty, "Backend Developer — Rīga!"),
            SubmittedJobFactory.ComputeExternalId(user, string.Empty, "backend developer riga"));
    }

    [Fact]
    public void Create_MarksJobAsSubmittedByUser()
    {
        var user = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var job = SubmittedJobFactory.Create(user, "https://www.linkedin.com/jobs/view/1/", "text", Extracted, now);

        Assert.Equal(user, job.SubmittedByUserId);
        Assert.Equal(SubmittedJobFactory.LinkedInSource, job.Source);
        Assert.Equal("Backend Developer", job.Title);
        Assert.Equal(3000m, job.SalaryMin);
        Assert.Equal(now, job.LastSeenAt);
    }
}
