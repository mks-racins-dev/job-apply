using JobMagnet.Scrapers.Parsing;

namespace JobMagnet.Tests.Scrapers;

public class ParserTests
{
    private const string VisiDarbiHtml = """
        <div class="items">
          <div class="item premium big-item">
            <div class="title"><h3><a class="long-title" href="/darba-sludinajums/setnieks-ce/6abd19e399a3c">SĒTNIEKS/-CE</a></h3></div>
            <div class="details"><ul>
              <li class="location"><span>Jelgava</span></li>
              <li class="salary"><span>1250 - 1400 <small>(bruto)</small></span></li>
              <li class="company"><span>EVOPIPES SIA</span></li>
            </ul></div>
          </div>
          <div class="item big-item">
            <div class="title"><h3><a href="/darba-sludinajums/elektriki/6ae2145b61151">ELEKTRIĶI</a></h3></div>
            <div class="details"><ul><li class="salary"><span>18 - 25 / st.</span></li></ul></div>
          </div>
        </div>
        """;

    private const string SsLvHtml = """
        <table><tr id="head_line"><td>Sludinājumi</td></tr>
        <tr id="tr_54499932"><td class="msga2 pp0"></td><td class="msga2"></td>
          <td class=msg2><div class=d1><a class="am" href="/msg/lv/work/are-required/printing-house-worker/mcmec.html"><b>Aicinām darbā Iespiedēju</b></a></div><div class="ads_region">Rīga, Dārzciems</div></td>
          <td class="msga2-o pp6"><b>Tipogrāfijas darbinieks</b></td><td class="msga2-o pp6"><b>Jaukti</b></td></tr>
        </table>
        """;

    private const string CvLvHtml = """
        <html><script id="__NEXT_DATA__" type="application/json">
        {"props":{"pageProps":{"searchResults":{"vacancies":[
          {"id":1674716,"positionTitle":"Finanšu produktu pārdošanas speciālists","positionContent":"Equilia Group apvieno...","employerName":"CV-Online Recruitment","publishDate":"2026-10-28T12:48:29.000+00:00","salaryFrom":1400,"salaryTo":2500,"hourlySalary":false},
          {"id":5,"positionTitle":"Kurjers","salaryFrom":8,"salaryTo":10,"hourlySalary":true}
        ]}}}}
        </script></html>
        """;

    [Fact]
    public void VisiDarbi_ParsesCards()
    {
        var jobs = VisiDarbiParser.Parse(VisiDarbiHtml);

        Assert.Equal(2, jobs.Count);
        var first = jobs[0];
        Assert.Equal("6abd19e399a3c", first.ExternalId);
        Assert.Equal("https://www.visidarbi.lv/darba-sludinajums/setnieks-ce/6abd19e399a3c", first.Url);
        Assert.Equal("SĒTNIEKS/-CE", first.Title);
        Assert.Equal("EVOPIPES SIA", first.Company);
        Assert.Equal("Jelgava", first.City);
        Assert.Equal(1250m, first.SalaryMin);
        Assert.Equal(1400m, first.SalaryMax);
        Assert.Null(jobs[1].SalaryMin);
    }

    [Fact]
    public void SsLv_ParsesRowsAndSkipsHeader()
    {
        var job = Assert.Single(SsLvParser.Parse(SsLvHtml));

        Assert.Equal("54499932", job.ExternalId);
        Assert.Equal("Tipogrāfijas darbinieks", job.Title);
        Assert.Equal("Rīga", job.City);
        Assert.Equal("Aicinām darbā Iespiedēju", job.Description);
    }

    [Fact]
    public void CvLv_ParsesNextData()
    {
        var jobs = CvLvParser.Parse(CvLvHtml);

        Assert.Equal(2, jobs.Count);
        Assert.Equal("1674716", jobs[0].ExternalId);
        Assert.Equal("https://www.cv.lv/lv/vacancy/1674716", jobs[0].Url);
        Assert.Equal("CV-Online Recruitment", jobs[0].Company);
        Assert.Equal(1400m, jobs[0].SalaryMin);
        Assert.Equal(2500m, jobs[0].SalaryMax);
        Assert.NotNull(jobs[0].PostedAt);
        Assert.Null(jobs[1].SalaryMin);
    }

    [Fact]
    public void CvLv_ReturnsEmptyWithoutNextData() => Assert.Empty(CvLvParser.Parse("<html></html>"));

    [Theory]
    [InlineData("1250 (bruto)", 1250, 1250)]
    [InlineData("1 200 - 2 000 EUR", 1200, 2000)]
    [InlineData("18 - 25 / st.", null, null)]
    [InlineData("", null, null)]
    public void SalaryParser_HandlesFormats(string text, int? min, int? max)
    {
        var (salaryMin, salaryMax) = SalaryParser.Parse(text);

        Assert.Equal((decimal?)min, salaryMin);
        Assert.Equal((decimal?)max, salaryMax);
    }
}
