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
            .OrderByDescending(e => e.EventId)
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
            //TODO: Test for N+1 and add if needed
            //.AsSplitQuery()
            .FirstOrDefaultAsync(e => e.EventId == eventId, ct);
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
