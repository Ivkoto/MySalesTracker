using MySalesTracker.Domain.Models;

namespace MySalesTracker.Application.Interfaces;

public interface IWeatherService
{
    Task<(double lat, double lon, string? name)?> FetchGeocode(string city, string? language = "bg");
    Task<WeatherForecast?> GetForecast(double lat, double lon, int forecastDays = 7);
}
