using Microsoft.EntityFrameworkCore;
using MySalesTracker.Data;
using MySalesTracker.Data.Models;

namespace MySalesTracker.Services;

public class EventService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<EventService> _logger;

    public EventService(IDbContextFactory<AppDbContext> contextFactory, ILogger<EventService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets all events with their days, ordered by EventId descending.
    /// </summary>
    public async Task<List<Event>> GetAllEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Events
                .Include(e => e.Days)
                .OrderByDescending(e => e.EventId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve events");
            throw;
        }
    }

    /// <summary>
    /// Creates a new event with the specified date range and returns it with generated event days.
    /// </summary>
    public async Task<Event> CreateEventAsync(string name, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Event name cannot be empty", nameof(name));

            if (endDate < startDate)
                throw new ArgumentException("End date cannot be before start date", nameof(endDate));

            _logger.LogInformation("Creating event '{Name}' from {StartDate} to {EndDate}", name, startDate, endDate);

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var evt = new Event { Name = name, StartDate = startDate, EndDate = endDate };

            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                evt.Days.Add(new EventDay { Date = d });
            }

            context.Events.Add(evt);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created event {EventId} with {DayCount} days", evt.EventId, evt.Days.Count);
            return evt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create event '{Name}'", name);
            throw;
        }
    }

    /// <summary>
    /// Gets a specific event day by ID, including the parent event information.
    /// Returns null if not found.
    /// </summary>
    public async Task<EventDay?> GetEventDayByIdAsync(int eventDayId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var evtDay = await context.EventDays
                .Include(ed => ed.Event)
                .FirstOrDefaultAsync(ed => ed.EventDayId == eventDayId, cancellationToken);

            if (evtDay == null)
            {
                _logger.LogWarning("EventDay with ID {EventDayId} not found", eventDayId);
            }

            return evtDay;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error while retrieving EventDay with ID {EventDayId}", eventDayId);
            throw;
        }
    }
}
