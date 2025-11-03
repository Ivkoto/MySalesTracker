using System.Net.Http.Json;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Models;

namespace MySalesTracker.Infrastructure.ExternalServices;

public sealed class WeatherService(HttpClient http) : IWeatherService
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
    public async Task<(double lat, double lon, string? name)?> FetchGeocode(string city, string? language = "bg")
    {
        if (string.IsNullOrWhiteSpace(city))
            return null;

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
    /// Retrieves weather forecast data from the Open-Meteo API for specified coordinates
    /// and transforms it into the domain model.
    /// </summary>
    /// <param name="lat">The latitude of the location.</param>
    /// <param name="lon">The longitude of the location.</param>
    /// <param name="forecastDays">The number of forecast days to retrieve (1-10, default: 7).</param>
    /// <returns>
    /// A <see cref="WeatherForecast"/> containing hourly forecast data including temperature,
    /// wind speed, and precipitation; or null if the request fails.
    /// </returns>
    public async Task<WeatherForecast?> GetForecast(double lat, double lon, int forecastDays = 7)
    {
        var days = Math.Clamp(forecastDays, 1, 10);
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&hourly=temperature_2m,wind_speed_10m,precipitation_probability,precipitation&temperature_unit=celsius&windspeed_unit=kmh&forecast_days={days}&timezone=auto";
        
        try
        {
            var response = await http.GetFromJsonAsync<ForecastResponse>(url);
            
            if (response?.Hourly == null) return null;
            
            // Transform parallel arrays from external API into domain model
            var hours = new List<HourlyForecast>();
            var hourly = response.Hourly;
            
            for (int i = 0; i < (hourly.Time?.Count ?? 0); i++)
            {
                hours.Add(new HourlyForecast(
                    Time: DateTime.Parse(hourly.Time![i]),
                    Temperature: hourly.Temperature_2m?[i] ?? 0,
                    WindSpeed: hourly.Wind_Speed_10m?[i] ?? 0,
                    PrecipitationProbability: hourly.Precipitation_Probability?[i] ?? 0,
                    Precipitation: hourly.Precipitation?[i] ?? 0
                ));
            }
            
            return new WeatherForecast(hours);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }    
}
