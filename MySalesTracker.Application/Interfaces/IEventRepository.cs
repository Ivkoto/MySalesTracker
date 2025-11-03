using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Application.Interfaces;

public interface IEventRepository
{
    Task<List<(string Name, DateOnly StartDate, DateOnly EndDate)>> GetExistingEventsByYear(int year, CancellationToken ct);
    Task<List<Event>> GetAllEvents(CancellationToken ct);
    Task<Event> CreateEvent(Event evt, CancellationToken ct);
    Task<EventDay?> GetEventDayById(int id, CancellationToken ct);
}
