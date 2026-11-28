namespace MySalesTracker.Web.State;

public sealed class WeatherState
{
    static readonly TimeSpan ForecastCacheLifetime = TimeSpan.FromMinutes(15);

    public string City { get; set; } = "Sofia";
    public int DisplayDays { get; set; } = 3;
    public Coordinates? CurrentLocation { get; set; }

    // Setup runs once per app session and completes only when a location is accepted,
    // so leaving during the first lookup lets the next visit finish the selection.
    public bool LocationAttempted { get; set; }

    public record Coordinates(double Latitude, double Longitude);

    public record HourEntry(DateTime Time, double Temp, double Wind, int Prob, double Mm);
    public record CurrentCondition(DateTime Time, double Temp, double Rainfall);
    public record DaySummary(DateOnly Date, List<HourEntry> Hours);
    public record Summary(
        string Name,
        double Lat,
        double Lon,
        List<DaySummary> Days,
        CurrentCondition? Current = null,
        string? TimeZone = null)
    {
        public DateTimeOffset CachedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    }

    public Summary? LastSummary { get; private set; }

    public Summary? GetFreshSummary()
        => LastSummary is not null
            && LastSummary.CachedAtUtc >= DateTimeOffset.UtcNow - ForecastCacheLifetime
                ? LastSummary
                : null;

    public void Store(Summary? summary) => LastSummary = summary;
}
