using System.Globalization;

namespace JobMagnet.Web.Components.Common;

public static class RelativeTime
{
    private const int MinutesPerHour = 60;
    private const int HoursPerDay = 24;
    private const int DaysShownRelative = 7;

    public static string Format(DateTimeOffset? at, DateTimeOffset now)
    {
        if (at is not { } time)
        {
            return "never";
        }

        var elapsed = now - time;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (elapsed.TotalMinutes < MinutesPerHour)
        {
            return $"{(int)elapsed.TotalMinutes} min ago";
        }

        if (elapsed.TotalHours < HoursPerDay)
        {
            return $"{(int)elapsed.TotalHours} h ago";
        }

        var days = (int)elapsed.TotalDays;
        return days switch
        {
            1 => "yesterday",
            < DaysShownRelative => $"{days} days ago",
            _ => time.ToLocalTime().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
        };
    }
}
