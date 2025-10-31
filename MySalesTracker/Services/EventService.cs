using Microsoft.EntityFrameworkCore;
using MySalesTracker.Data;
using MySalesTracker.Data.Models;
using MySalesTracker.Domain;

namespace MySalesTracker.Services;

public sealed class EventService(IDbContextFactory<AppDbContext> contextFactory, ILogger<EventService> logger)
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
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var events = await context.Events
                .Where(e => e.StartDate.Year == year.Year || e.EndDate.Year == year.Year)
                .Select(e => new { e.Name, e.StartDate, e.EndDate })
                .ToListAsync(cancellationToken);

            return [.. events.Select(e => (e.Name, e.StartDate, e.EndDate))];
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
    public async Task<List<Event>> GetAllEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Events
                .Include(e => e.Days)
                .OrderByDescending(e => e.EventId)
                .ToListAsync(cancellationToken);
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
    public async Task<(bool Success, Event? Event, string? ErrorMessage)> CreateEventAsync(string name, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingEvents = await GetExistingEventsByYear(startDate, cancellationToken);

            var validation = EventValidations.ValidateCreateEvent(name, startDate, endDate, existingEvents);
            if (!validation.IsValid)
                return (false, null, validation.ErrorMessage);

            logger.LogInformation("Creating event '{Name}' from {StartDate} to {EndDate}", name, startDate, endDate);

            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var evt = new Event { Name = name, StartDate = startDate, EndDate = endDate };

            var dates = EventValidations.GenerateDateRange(startDate, endDate);
            foreach (var date in dates)
            {
                evt.Days.Add(new EventDay { Date = date });
            }

            context.Events.Add(evt);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully created event {EventId} with {DayCount} days", evt.EventId, evt.Days.Count);
            return (true, evt, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create event '{Name}'", name);
            throw;
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
    public async Task<EventDay?> GetEventDayByIdAsync(int eventDayId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var evtDay = await context.EventDays
                .Include(ed => ed.Event)
                .FirstOrDefaultAsync(ed => ed.EventDayId == eventDayId, cancellationToken);

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
