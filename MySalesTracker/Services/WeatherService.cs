namespace MySalesTracker.Services;

public sealed class WeatherService
{
    private readonly HttpClient _http;
    public WeatherService(HttpClient http) => _http = http;

    public async Task<(double lat, double lon, string? name)?> GeocodeAsync(string city, string? language = "bg")
    {
        if (string.IsNullOrWhiteSpace(city)) return null;
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language={language}";
        try
        {
            var data = await _http.GetFromJsonAsync<GeocodeResponse>(url);
            var first = data?.Results?.FirstOrDefault();
            return first is null ? null : (first.Latitude, first.Longitude, first.Name);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ForecastResponse?> GetForecastAsync(double lat, double lon)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&hourly=temperature_2m,wind_speed_10m,precipitation_probability,precipitation&temperature_unit=celsius&windspeed_unit=kmh&timezone=auto";
        try
        {
            return await _http.GetFromJsonAsync<ForecastResponse>(url);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

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

    public sealed class Hourly
    {
        public List<string>? Time { get; set; }
        public List<int>? Precipitation_Probability { get; set; }
        public List<double>? Precipitation { get; set; }
        public List<double>? Temperature_2m { get; set; }
        public List<double>? Wind_Speed_10m { get; set; }
    }
}
