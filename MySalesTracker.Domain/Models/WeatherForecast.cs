namespace MySalesTracker.Domain.Models;

public record WeatherForecast(
    List<HourlyForecast> Hours,
    CurrentWeather? Current = null,
    string? TimeZone = null);
public record HourlyForecast(DateTime Time, double Temperature, double WindSpeed, int PrecipitationProbability, double Precipitation);
public record CurrentWeather(DateTime Time, double Temperature, double Rainfall);
