using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Application.Interfaces;

public interface IEventRepository
{
    Task<List<(string Name, DateOnly StartDate, DateOnly EndDate)>> GetExistingEventsByYearAsync(int year, CancellationToken ct);
    Task<List<Event>> GetAllEventsAsync(CancellationToken ct);
    Task<Event> CreateEventAsync(Event evt, CancellationToken ct);
    Task<EventDay?> GetEventDayByIdAsync(int id, CancellationToken ct);
    Task<Event?> GetEventWithAllDataAsync(int eventId, CancellationToken ct);
    Task<List<Event>> GetEventsWithAllDataAsync(IReadOnlyCollection<int> eventIds, CancellationToken ct);
    Task<Event?> GetEventWithDaysOnlyAsync(int eventId, CancellationToken ct);
    Task<List<EventDay>> GetEventDaysWithDataAsync(int eventId, IReadOnlyCollection<int> dayIds, CancellationToken ct);
    Task<EventDay?> UpdateStartingPettyCashAsync(int eventDayId, decimal? amount, CancellationToken ct);
}
