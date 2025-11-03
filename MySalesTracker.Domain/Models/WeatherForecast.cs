namespace MySalesTracker.Domain.Models;

public record WeatherForecast(List<HourlyForecast> Hours);
public record HourlyForecast(DateTime Time, double Temperature, double WindSpeed, int PrecipitationProbability, double Precipitation);
