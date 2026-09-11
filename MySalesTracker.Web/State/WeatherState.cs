namespace MySalesTracker.Web.State;

public sealed class WeatherState
{
    public string City { get; set; } = "Sofia";
    public int DisplayDays { get; set; } = 3;
    public Coordinates? CurrentLocation { get; set; }

    public record Coordinates(double Latitude, double Longitude);

    public record HourEntry(DateTime Time, double Temp, double Wind, int Prob, double Mm);
    public record CurrentCondition(DateTime Time, double Temp, double Rainfall);
    public record DaySummary(DateOnly Date, List<HourEntry> Hours);
    public record Summary(
        string Name,
        double Lat,
        double Lon,
        List<DaySummary> Days,
        CurrentCondition? Current = null);

    public Summary? LastSummary { get; private set; }

    public void Store(Summary? summary) => LastSummary = summary;
}
