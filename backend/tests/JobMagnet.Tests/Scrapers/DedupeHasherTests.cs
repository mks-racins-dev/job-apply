using JobMagnet.Scrapers;
using JobMagnet.Scrapers.Ingestion;

namespace JobMagnet.Tests.Scrapers;

public class DedupeHasherTests
{
    [Fact]
    public void SameVacancyOnDifferentSites_HasSameHash()
    {
        var visiDarbi = CreateJob("visidarbi.lv", "SĒTNIEKS/-CE", "EVOPIPES SIA", "Jelgava");
        var cvLv = CreateJob("cv.lv", "Sētnieks / -ce", "Evopipes SIA", "jelgava");

        Assert.Equal(DedupeHasher.Compute(visiDarbi), DedupeHasher.Compute(cvLv));
    }

    [Fact]
    public void DifferentCompany_HasDifferentHash()
    {
        var first = CreateJob("cv.lv", "Sētnieks", "A SIA", "Rīga");
        var second = CreateJob("cv.lv", "Sētnieks", "B SIA", "Rīga");

        Assert.NotEqual(DedupeHasher.Compute(first), DedupeHasher.Compute(second));
    }

    private static ScrapedJob CreateJob(string source, string title, string company, string city) =>
        new(source, "1", "https://example.com", title, company, city, null, null, string.Empty, null);
}
