using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Application.Interfaces;

/// <summary>
/// Repository for managing events and loading event-day sales and payments.
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Retrieves the names and dates of events that start or end in the specified year.
    /// </summary>
    Task<List<(string Name, DateOnly StartDate, DateOnly EndDate)>> GetExistingEventsByYearAsync(int year, CancellationToken ct);

    /// <summary>
    /// Retrieves all events with their event days.
    /// </summary>
    Task<List<Event>> GetAllEventsAsync(CancellationToken ct);

    /// <summary>
    /// Saves a new event with its associated days.
    /// </summary>
    Task<Event> CreateEventAsync(Event evt, CancellationToken ct);

    /// <summary>
    /// Finds an event day by ID and includes its parent event.
    /// </summary>
    Task<EventDay?> GetEventDayByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Loads an event with its days, sales, sold products, and payments.
    /// </summary>
    Task<Event?> GetEventWithAllDataAsync(int eventId, CancellationToken ct);

    /// <summary>
    /// Loads the specified events with their days, sales, sold products, and payments.
    /// </summary>
    Task<List<Event>> GetEventsWithAllDataAsync(IReadOnlyCollection<int> eventIds, CancellationToken ct);

    /// <summary>
    /// Loads an event and its day records.
    /// </summary>
    Task<Event?> GetEventWithDaysOnlyAsync(int eventId, CancellationToken ct);

    /// <summary>
    /// Loads the specified days within an event with their sales, sold products, and payments.
    /// </summary>
    Task<List<EventDay>> GetEventDaysWithDataAsync(int eventId, IReadOnlyCollection<int> dayIds, CancellationToken ct);

    /// <summary>
    /// Sets or clears the starting petty cash amount for an event day.
    /// </summary>
    Task<EventDay?> UpdateStartingPettyCashAsync(int eventDayId, decimal? amount, CancellationToken ct);
}
