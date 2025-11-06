namespace MySalesTracker.Domain.Services;

/// <summary>
/// Pure calculation functions for weather-related operations.
/// Contains precipitation color mapping and data transformation logic.
/// </summary>
public static class WeatherCalculations
{
    /// <summary>
    /// Value between 0 and 1.
    /// </summary>
    private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;

    /// <summary>
    /// Linear interpolation between two values.
    /// </summary>
    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    /// <summary>
    /// HSL hue value for a given precipitation amount.
    /// Maps precipitation range to color: yellow (no rain) → blue (heavy rain).
    /// </summary>
    /// <param name="mm">Precipitation amount in millimeters.</param>
    /// <returns>Hue value in range 60-210 (yellow to blue).</returns>
    private static int HueForRain(double mm)
    {
        const double MaxRain = 2.0;
        var t = Clamp01(mm / MaxRain);
        var hue = Lerp(60, 210, t);
        return (int)Math.Round(hue);
    }

    /// <summary>
    /// Background color (HSLA) for a precipitation value.
    /// </summary>
    /// <param name="mm">Precipitation amount in millimeters.</param>
    /// <returns>HSLA color string for CSS.</returns>
    public static string GetRainBackgroundColor(double mm)
    {
        var hue = HueForRain(mm);
        var saturation = hue > 150 ? 95 : 85;
        var lightness = hue > 150 ? 38 : 50;
        var alpha = hue > 150 ? 0.6 : 0.4;
        return $"hsla({hue}, {saturation}%, {lightness}%, {alpha})";
    }

    /// <summary>
    /// Border color (HSL) for a precipitation value.
    /// </summary>
    /// <param name="mm">Precipitation amount in millimeters.</param>
    /// <returns>HSL color string for CSS.</returns>
    public static string GetRainBorderColor(double mm)
    {
        var hue = HueForRain(mm);
        var saturation = hue > 150 ? 85 : 70;
        var lightness = hue > 150 ? 28 : 40;
        return $"hsl({hue}, {saturation}%, {lightness}%)";
    }

    /// <summary>
    /// Text color for a precipitation value to ensure readability.
    /// </summary>
    /// <param name="mm">Precipitation amount in millimeters.</param>
    /// <returns>CSS color string for text.</returns>
    public static string GetRainTextColor(double mm)
    {
        var hue = HueForRain(mm);
        return $"hsl({hue}, 30%, 15%)";
    }
}
