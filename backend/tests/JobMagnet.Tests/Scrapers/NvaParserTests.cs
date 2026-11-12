using JobMagnet.Scrapers.Parsing;

namespace JobMagnet.Tests.Scrapers;

public class NvaParserTests
{
    private const string SearchJson = """
        {"success": true, "result": {"total": 2, "records": [
          {"_id":1,"Vakances_Nr":"261104-12","Aktualizacijas_datums":"2026-11-04T00:00:00","Iestades_registracijas_numurs":40003575567,"Vakances_nosaukums":"AUTOMOBIĻA VADĪTĀJS","Vakances_kategorija":"Elektronika / Enerģētika / Elektroenerģija","Alga_no":1774.000,"Alga_lidz":1774.000,"Slodzes_tips":"Viena vesela slodze","Darba_laika_veids":"Normālais darba laiks","Darba_stundas_nedela":null,"Pieteiksanas_termins":"2026-11-15T00:00:00","Attels":"","Vieta":"Dārzciema iela 86, Rīga","Vakances_paplasinats_apraksts":"https://cvvp.nva.gov.lv/#/pub/vakances/468324505"},
          {"_id":2,"Vakances_Nr":"261110-138","Aktualizacijas_datums":"2026-11-10T00:00:00","Vakances_nosaukums":"SĒTNIEKS","Alga_no":0,"Alga_lidz":null,"Vieta":"Riekstu iela 2, Lielvārde, Ogres nov.","Vakances_paplasinats_apraksts":"https://cvvp.nva.gov.lv/#/pub/vakances/468950380"}
        ]}}
        """;

    [Fact]
    public void Parse_MapsRecords()
    {
        var page = NvaParser.Parse(SearchJson);

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Jobs.Count);

        var first = page.Jobs[0];
        Assert.Equal("nva.gov.lv", first.Source);
        Assert.Equal("261104-12", first.ExternalId);
        Assert.Equal("https://cvvp.nva.gov.lv/#/pub/vakances/468324505", first.Url);
        Assert.Equal("AUTOMOBIĻA VADĪTĀJS", first.Title);
        Assert.Equal("Rīga", first.City);
        Assert.Equal(1774m, first.SalaryMin);
        Assert.Equal(1774m, first.SalaryMax);
        Assert.Equal(new DateTimeOffset(2026, 11, 4, 0, 0, 0, TimeSpan.Zero), first.PostedAt);
        Assert.Equal(new DateTimeOffset(2026, 11, 15, 0, 0, 0, TimeSpan.Zero), first.Deadline);
        Assert.Contains("Pieteikšanās termiņš: 2026-11-15", first.Description);
        Assert.Contains("40003575567", first.Description);

        var second = page.Jobs[1];
        Assert.Equal("Lielvārde", second.City);
        Assert.Null(second.SalaryMin);
        Assert.Null(second.SalaryMax);
    }

    [Fact]
    public void ParseResourceId_PicksDatastoreResource()
    {
        const string json = """
            {"result": {"resources": [
              {"id": "old", "datastore_active": false},
              {"id": "7f68f6fc", "datastore_active": true}
            ]}}
            """;

        Assert.Equal("7f68f6fc", NvaParser.ParseResourceId(json));
    }
}
