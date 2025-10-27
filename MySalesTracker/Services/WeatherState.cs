namespace MySalesTracker.Services;

public sealed class WeatherState
{
    public string City { get; set; } = "София";
    public int DisplayHours { get; set; } = 12;

    public record Entry(string Display, double Temp, double Wind, int Prob, double Mm);
    public record Summary(string Name, double Lat, double Lon, List<Entry> Items);

    public Summary? LastSummary { get; private set; }

    public void Store(Summary summary)
        => LastSummary = summary;
}
