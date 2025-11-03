namespace MySalesTracker.Web.State;

public sealed class WeatherState
{
    public string City { get; set; } = "Sofia";
    public int DisplayDays { get; set; } = 3;

    public record HourEntry(string TimeLabel, double Temp, double Wind, int Prob, double Mm);
    public record DaySummary(string DayLabel, List<HourEntry> Hours);
    public record Summary(string Name, double Lat, double Lon, List<DaySummary> Days);

    public Summary? LastSummary { get; private set; }

    public void Store(Summary summary)
        => LastSummary = summary;
}
