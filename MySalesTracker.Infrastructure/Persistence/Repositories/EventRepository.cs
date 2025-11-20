using Microsoft.EntityFrameworkCore;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Infrastructure.Persistence.Repositories;

internal class EventRepository(IDbContextFactory<AppDbContext> contextFactory) : IEventRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory = contextFactory;

    public async Task<List<(string Name, DateOnly StartDate, DateOnly EndDate)>> GetExistingEventsByYear(int year, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var events = await context.Events
            .Where(e => e.StartDate.Year == year || e.EndDate.Year == year)
            .Select(e => new { e.Name, e.StartDate, e.EndDate })
            .ToListAsync(ct);

        return [.. events.Select(e => (e.Name, e.StartDate, e.EndDate))];
    }

    public async Task<Event> CreateEvent(Event evt, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        context.Events.Add(evt);
        await context.SaveChangesAsync(ct);

        return evt;
    }

    public async Task<List<Event>> GetAllEvents(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Events
            .Include(e => e.Days)
            .OrderByDescending(e => e.EventId)
            .ToListAsync(ct);
    }

    public async Task<EventDay?> GetEventDayById(int id, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var evtDay = await context.EventDays
                .Include(ed => ed.Event)
                .FirstOrDefaultAsync(ed => ed.EventDayId == id, ct);

        return evtDay;
    }

    public async Task<Event?> GetEventWithAllData(int eventId, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Events
            .Include(e => e.Days)
                .ThenInclude(d => d.Sales)
                    .ThenInclude(s => s.Product)
            .Include(e => e.Days)
                .ThenInclude(d => d.PaymentsCounted)
            //TODO: Test and add if needed
            //.AsSplitQuery()
            .FirstOrDefaultAsync(e => e.EventId == eventId, ct);
    }

    public async Task<EventDay?> UpdateStartingPettyCash(int eventDayId, decimal? amount, CancellationToken ct)
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
