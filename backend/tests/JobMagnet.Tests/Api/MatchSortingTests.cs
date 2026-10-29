using JobMagnet.Api.Matches;
using JobMagnet.Data.Entities;

namespace JobMagnet.Tests.Api;

public class MatchSortingTests
{
    private static readonly Match HighPaid = CreateMatch(60, "Backend developer", salary: 4000);
    private static readonly Match LowPaid = CreateMatch(90, "Analyst", salary: 2000);
    private static readonly Match NoSalary = CreateMatch(75, "Cloud engineer", salary: null);

    private static readonly Match[] All = [HighPaid, LowPaid, NoSalary];

    [Fact]
    public void DefaultsToScoreDescending() =>
        Assert.Equal([LowPaid, NoSalary, HighPaid], Sort(null, null));

    [Fact]
    public void SortsByTitleAscending() =>
        Assert.Equal([LowPaid, HighPaid, NoSalary], Sort("title", "asc"));

    [Theory]
    [InlineData("desc")]
    [InlineData("asc")]
    public void Salary_MissingValuesAlwaysLast(string dir) =>
        Assert.Same(NoSalary, Sort("salary", dir).Last());

    [Fact]
    public void Salary_DescendingPutsHighestFirst() =>
        Assert.Equal([HighPaid, LowPaid, NoSalary], Sort("salary", "desc"));

    [Fact]
    public void UnknownSortFallsBackToScore() =>
        Assert.Equal(Sort(null, null), Sort("drop table", "desc"));

    private static List<Match> Sort(string? sort, string? dir) =>
        MatchSorting.Apply(All.AsQueryable(), sort, dir).ToList();

    private static Match CreateMatch(int score, string title, decimal? salary) => new()
    {
        Id = Guid.NewGuid(),
        Score = score,
        Reason = "reason",
        CreatedAt = DateTimeOffset.UtcNow,
        Job = new Job
        {
            Source = "test",
            ExternalId = Guid.NewGuid().ToString(),
            Url = "https://example.com",
            Title = title,
            SalaryMax = salary,
            DedupeHash = "hash"
        }
    };
}
