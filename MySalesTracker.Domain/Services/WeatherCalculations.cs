namespace MySalesTracker.Domain.Services;

/// <summary>
/// Pure calculation functions for weather-related operations.
/// Contains temperature color mapping and data transformation logic.
/// </summary>
public static class WeatherCalculations
{
    private const double MinTemp = -20;
    private const double MidTemp = 20;
    private const double MaxTemp = 40;

    /// <summary>
    /// Clamps a value between 0 and 1.
    /// </summary>
    private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;

    /// <summary>
    /// Linear interpolation between two values.
    /// </summary>
    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    /// <summary>
    /// Calculates the HSL hue value for a given temperature.
    /// Maps temperature range to color: blue (cold) → yellow (mid) → red (hot).
    /// </summary>
    /// <param name="temp">Temperature in Celsius.</param>
    /// <returns>Hue value (0-360) for HSL color.</returns>
    private static int HueForTemperature(double temp)
    {
        var t = Clamp01((temp - MinTemp) / (MaxTemp - MinTemp));
        var m = Clamp01((MidTemp - MinTemp) / (MaxTemp - MinTemp));
        double hue;

        if (t <= m)
        {
            var frac = Math.Abs(m) < double.Epsilon ? 0 : t / m;
            hue = Lerp(220, 60, frac); // Blue to yellow
        }
        else
        {
            var frac = Math.Abs(1 - m) < double.Epsilon ? 1 : (t - m) / (1 - m);
            hue = Lerp(60, 0, frac); // Yellow to red
        }

        return (int)Math.Round(hue);
    }

    /// <summary>
    /// Gets the background color (HSLA) for a temperature value.
    /// </summary>
    /// <param name="temp">Temperature in Celsius.</param>
    /// <returns>HSLA color string for CSS.</returns>
    public static string GetTemperatureBackgroundColor(double temp)
        => $"hsla({HueForTemperature(temp)}, 90%, 50%, 0.3)";

    /// <summary>
    /// Gets the border color (HSL) for a temperature value.
    /// </summary>
    /// <param name="temp">Temperature in Celsius.</param>
    /// <returns>HSL color string for CSS.</returns>
    public static string GetTemperatureBorderColor(double temp)
      => $"hsl({HueForTemperature(temp)}, 60%, 40%)";
}
