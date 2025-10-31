namespace MySalesTracker.Services;

public sealed class WeatherService(HttpClient http)
{
    /// <summary>
    /// Geocodes a city name to geographic coordinates using the Open-Meteo Geocoding API.
    /// </summary>
    /// <param name="city">The city name to geocode.</param>
    /// <param name="language">The language code for localized results (default: "bg" for Bulgarian).</param>
    /// <returns>
    /// A tuple containing latitude, longitude, and city name if found; otherwise null.
    /// Returns null if the city is empty or not found in the geocoding service.
    /// </returns>
    public async Task<(double lat, double lon, string? name)?> GeocodeAsync(string city, string? language = "bg")
    {
        if (string.IsNullOrWhiteSpace(city)) return null;
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language={language}";
        try
        {
            var data = await http.GetFromJsonAsync<GeocodeResponse>(url);
            var first = data?.Results?.FirstOrDefault();
            return first is null ? null : (first.Latitude, first.Longitude, first.Name);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>
    /// Retrieves weather forecast data from the Open-Meteo API for specified coordinates.
    /// </summary>
    /// <param name="lat">The latitude of the location.</param>
    /// <param name="lon">The longitude of the location.</param>
    /// <param name="forecastDays">The number of forecast days to retrieve (1-10, default: 7).</param>
    /// <returns>
    /// A <see cref="ForecastResponse"/> containing hourly forecast data including temperature,
    /// wind speed, and precipitation; or null if the request fails.
    /// </returns>
    public async Task<ForecastResponse?> GetForecastAsync(double lat, double lon, int forecastDays = 7)
    {
        var days = Math.Clamp(forecastDays, 1, 10);
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&hourly=temperature_2m,wind_speed_10m,precipitation_probability,precipitation&temperature_unit=celsius&windspeed_unit=kmh&forecast_days={days}&timezone=auto";
        try
        {
            return await http.GetFromJsonAsync<ForecastResponse>(url);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>
    /// Response model for geocoding API results.
    /// </summary>
    public sealed class GeocodeResponse
    {
        public List<GeocodeItem>? Results { get; set; }
    }

    /// <summary>
    /// Represents a single geocoding result with location coordinates and metadata.
    /// </summary>
    public sealed class GeocodeItem
    {
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
        public string? Name { get; set; }
        public string? Country { get; set; }
    }

    /// <summary>
    /// Response model for weather forecast API containing hourly data.
    /// </summary>
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
}
