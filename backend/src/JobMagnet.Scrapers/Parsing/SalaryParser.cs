using System.Globalization;
using System.Text.RegularExpressions;

namespace JobMagnet.Scrapers.Parsing;

public static partial class SalaryParser
{
    // Hourly rates are dropped so every stored salary is comparable as a monthly figure.
    public static (decimal? Min, decimal? Max) Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || HourlyRegex().IsMatch(text))
        {
            return (null, null);
        }

        var numbers = NumberRegex().Matches(text.Replace(" ", string.Empty))
            .Select(m => decimal.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture))
            .ToList();

        return numbers.Count switch
        {
            0 => (null, null),
            1 => (numbers[0], numbers[0]),
            _ => (numbers.Min(), numbers.Max())
        };
    }

    [GeneratedRegex(@"\d+(?:[.,]\d+)?")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"/\s*st\.|/\s*h\b|stundā", RegexOptions.IgnoreCase)]
    private static partial Regex HourlyRegex();
}
