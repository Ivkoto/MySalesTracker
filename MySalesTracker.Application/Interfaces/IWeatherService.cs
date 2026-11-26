using MySalesTracker.Domain.Models;

namespace MySalesTracker.Application.Interfaces;

public interface IWeatherService
{
    /// <summary>
    /// Finds coordinates and, when available, a localized name for a city.
    /// </summary>
    Task<(double lat, double lon, string? name)?> FetchGeocode(string city, string? language = "bg");

    /// <summary>
    /// Retrieves current conditions and an hourly forecast for the specified coordinates.
    /// </summary>
    Task<WeatherForecast?> GetForecast(double lat, double lon, int forecastDays = 7);
}
