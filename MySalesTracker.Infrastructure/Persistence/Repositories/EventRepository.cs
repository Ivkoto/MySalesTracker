using Microsoft.EntityFrameworkCore;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Infrastructure.Persistence.Repositories;

internal class EventRepository(IDbContextFactory<AppDbContext> contextFactory) : IEventRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory = contextFactory;

    public async Task<List<(string Name, DateOnly StartDate, DateOnly EndDate)>> GetExistingEventsByYearAsync(int year, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var events = await context.Events
            .Where(e => e.StartDate.Year == year || e.EndDate.Year == year)
            .Select(e => new { e.Name, e.StartDate, e.EndDate })
            .OrderByDescending(e => e.EndDate)
            .ToListAsync(ct);

        return [.. events.Select(e => (e.Name, e.StartDate, e.EndDate))];
    }

    public async Task<Event> CreateEventAsync(Event evt, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        context.Events.Add(evt);
        await context.SaveChangesAsync(ct);

        return evt;
    }

    public async Task<List<Event>> GetAllEventsAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Events
            .Include(e => e.Days)
            .OrderByDescending(e => e.EndDate)
            .ToListAsync(ct);
    }

    public async Task<EventDay?> GetEventDayByIdAsync(int id, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var evtDay = await context.EventDays
                .Include(ed => ed.Event)
                .FirstOrDefaultAsync(ed => ed.EventDayId == id, ct);

        return evtDay;
    }

    public async Task<Event?> GetEventWithAllDataAsync(int eventId, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Events
            .AsNoTracking()
            .Include(e => e.Days)
                .ThenInclude(d => d.Sales)
                    .ThenInclude(s => s.Product)
            .Include(e => e.Days)
                .ThenInclude(d => d.Payments)
            //TODO: Enable if verify through testing that the query doesn't cause performance issues with realistic data volumes.
            //.AsSplitQuery()
            .FirstOrDefaultAsync(e => e.EventId == eventId, ct);
    }

    public async Task<List<Event>> GetEventsWithAllDataAsync(IReadOnlyCollection<int> eventIds, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Events
            .AsNoTracking()
            .Where(e => eventIds.Contains(e.EventId))
            .Include(e => e.Days)
                .ThenInclude(d => d.Sales)
                    .ThenInclude(s => s.Product)
            .Include(e => e.Days)
                .ThenInclude(d => d.Payments)
            .OrderByDescending(e => e.EndDate)
            //TODO: Enable if verify through testing that the query doesn't cause performance issues with realistic data volumes.
            //.AsSplitQuery()
            .ToListAsync(ct);
    }

    public async Task<Event?> GetEventWithDaysOnlyAsync(int eventId, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Events
            .AsNoTracking()
            .Include(e => e.Days)
            .FirstOrDefaultAsync(e => e.EventId == eventId, ct);
    }

    public async Task<List<EventDay>> GetEventDaysWithDataAsync(int eventId, IReadOnlyCollection<int> dayIds, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.EventDays
            .AsNoTracking()
            .Where(d => d.EventId == eventId && dayIds.Contains(d.EventDayId))
            .Include(d => d.Sales)
                .ThenInclude(s => s.Product)
            .Include(d => d.Payments)
            .ToListAsync(ct);
    }    

    public async Task<EventDay?> UpdateStartingPettyCashAsync(int eventDayId, decimal? amount, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var eventDay = await context.EventDays
            .FirstOrDefaultAsync(ed => ed.EventDayId == eventDayId, ct);

        if (eventDay is null)
        {
            return null;
        }

        eventDay.StartingPettyCash = amount;
        await context.SaveChangesAsync(ct);

        return eventDay;
    }
}
