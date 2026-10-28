namespace JobMagnet.Web.Api;

public class ProfilePreferences
{
    public bool Remote { get; set; }

    public List<string> Cities { get; set; } = [];

    public decimal? SalaryMin { get; set; }
}
