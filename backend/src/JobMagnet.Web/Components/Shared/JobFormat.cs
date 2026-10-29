using JobMagnet.Web.Api;

namespace JobMagnet.Web.Components.Shared;

public static class JobFormat
{
    public static string Salary(JobItem job) => (job.SalaryMin, job.SalaryMax) switch
    {
        (null, null) => "Not stated",
        ({ } min, { } max) when min != max => $"€{min:0} – €{max:0} / month",
        _ => $"€{job.SalaryMin ?? job.SalaryMax:0} / month"
    };

    public static string Source(JobItem job) => job.Source switch
    {
        "linkedin.com" => "LinkedIn (added by you)",
        "manual" => "Added by you",
        _ => job.Source
    };
}
