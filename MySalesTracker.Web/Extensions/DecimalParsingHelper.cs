using System.Globalization;

namespace MySalesTracker.Web.Extensions;

public static class DecimalParsingHelper
{
    private static readonly CultureInfo BulgarianCulture = CultureInfo.GetCultureInfo("bg-BG");

    public static bool TryParseDecimal(string? value, out decimal result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0;
            return false;
        }

        if (decimal.TryParse(value.Replace(" ", string.Empty), NumberStyles.Number, BulgarianCulture, out result))
        {
            return true;
        }

        var normalized = value.Replace(" ", string.Empty).Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }
}
 