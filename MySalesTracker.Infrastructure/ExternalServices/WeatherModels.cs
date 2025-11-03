namespace MySalesTracker.Infrastructure.ExternalServices;

public sealed class GeocodeResponse
{
    public List<GeocodeItem>? Results { get; set; }
}

public sealed class GeocodeItem
{
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public string? Name { get; set; }
    public string? Country { get; set; }
}

public sealed class ForecastResponse
{
    public Hourly? Hourly { get; set; }
}

/// <summary>
/// Hourly weather data including temperature, wind, and precipitation metrics.
/// All lists are aligned by index (same length) representing sequential hourly measurements.
/// </summary>
public sealed class Hourly
{
    public List<string>? Time { get; set; }
    public List<int>? Precipitation_Probability { get; set; }
    public List<double>? Precipitation { get; set; }
    public List<double>? Temperature_2m { get; set; }
    public List<double>? Wind_Speed_10m { get; set; }
}
