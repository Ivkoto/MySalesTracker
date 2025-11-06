using Microsoft.Extensions.Logging;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Services;

namespace MySalesTracker.Application.Services;

public sealed class EventService(IEventRepository eventRepository, ILogger<EventService> logger)
{
    /// <summary>
    /// Gets all events that overlap with the specified year.
    /// </summary>
    /// <param name="year">The year to filter events by. Only the year component is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of tuples containing the event name, start date, and end date for all events
    /// where either the start date or end date falls within the specified year.
    /// Returns an empty list if no events match the criteria.
    /// </returns>
    public async Task<List<(string, DateOnly, DateOnly)>> GetExistingEventsByYear(DateOnly year, CancellationToken cancellationToken = default)
    {
        try
        {
            return await eventRepository.GetExistingEventsByYear(year.Year, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve events");
            throw;
        }

    }

    /// <summary>
    /// Gets all events with their days, ordered by EventId descending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="Event"/> objects with their associated days loaded,
    /// ordered by most recent event first. Returns an empty list if no events exist.
    /// </returns>
    public async Task<List<Event>> GetAllEventsAsync(CancellationToken ct = default)
    {
        try
        {
            return await eventRepository.GetAllEvents(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve events");
            throw;
        }
    }

    /// <summary>
    /// Creates a new event with the specified date range and returns it with generated event days.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <param name="startDate">The start date of the event.</param>
    /// <param name="endDate">The end date of the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The newly created <see cref="Event"/> with auto-generated <see cref="EventDay"/> entries
    /// for each day in the specified range (inclusive).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when name is empty or whitespace, or when end date is before start date.
    /// </exception>
    public async Task<(bool Success, Event? Event, string? ErrorMessage)> CreateEventAsync(string name, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        try
        {
            var existingEvents = await GetExistingEventsByYear(startDate, ct);

            var validation = EventValidations.ValidateCreateEvent(name, startDate, endDate, existingEvents);
            if (!validation.IsValid)
                return (false, null, validation.ErrorMessage);

            logger.LogInformation("Creating event '{Name}' from {StartDate} to {EndDate}", name, startDate, endDate);

            var evt = new Event { Name = name, StartDate = startDate, EndDate = endDate };
            var dates = EventValidations.GenerateDateRange(startDate, endDate);
            foreach (var date in dates)
            {
                evt.Days.Add(new EventDay { Date = date });
            }

            var savedEvent = await eventRepository.CreateEvent(evt, ct);

            logger.LogInformation("Successfully created event {EventId} with {DayCount} days", savedEvent.EventId, savedEvent.Days.Count);
            return (true, savedEvent, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create event '{Name}'", name);
            return (false, null, "Failed to create event due to an error.");
        }
    }

    /// <summary>
    /// Gets a specific event day by ID, including the parent event information.
    /// </summary>
    /// <param name="eventDayId">The ID of the event day to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="EventDay"/> with the parent <see cref="Event"/> loaded if found;
    /// otherwise null. A warning is logged when the event day is not found.
    /// </returns>
    public async Task<EventDay?> GetEventDayByIdAsync(int eventDayId, CancellationToken ct = default)
    {
        try
        {
            var evtDay = await eventRepository.GetEventDayById(eventDayId, ct);

            if (evtDay == null)
            {
                logger.LogWarning("EventDay with ID {EventDayId} not found", eventDayId);
            }

            return evtDay;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database error while retrieving EventDay with ID {EventDayId}", eventDayId);
            throw;
        }
    }
}
