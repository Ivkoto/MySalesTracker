namespace MySalesTracker.Domain;

/// <summary>
/// Pure validation functions for event date logic.
/// Contains no state or dependencies—testable in isolation.
/// </summary>
public static class EventValidations
{
    /// <summary>
    /// Validates event name and date range.
    /// </summary>
    /// <param name="name">Event name.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="existingEvents">List of existing events to check for duplicates.</param>
    /// <returns>Validation result with error message if invalid.</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateCreateEvent(string name, DateOnly startDate, DateOnly endDate, List<(string Name, DateOnly StartDate, DateOnly EndDate)> existingEvents)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Името на събитието не може да бъде празно!");

        if (endDate < startDate)
            return (false, "Крайната дата не може да бъде преди началната дата!");

        var duplicate = existingEvents.FirstOrDefault(e =>
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            e.StartDate == startDate &&
            e.EndDate == endDate);

        if (duplicate != default)
            return (false, $"Събитие с име '{name}' за дати '{startDate}' -> '{endDate}' вече съществува.");

        return (true, null);
    }

    /// <summary>
    /// Generates event days for a date range (inclusive).
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <returns>List of dates in the range.</returns>
    public static List<DateOnly> GenerateDateRange(DateOnly startDate, DateOnly endDate)
    {
        var dates = new List<DateOnly>();
        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            dates.Add(d);
        }
        return dates;
    }

    /// <summary>
    /// Calculates the number of days in an event.
    /// </summary>
    public static int CalculateEventDuration(DateOnly startDate, DateOnly endDate)
        => (endDate.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
}
