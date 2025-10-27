using Microsoft.AspNetCore.SignalR;

namespace MySalesTracker.Hubs;

/// <summary>
/// SignalR hub responsible for broadcasting sales updates to connected clients.
/// </summary>
public class SalesHub : Hub
{
    public const string HubPath = "/hubs/sales";

    public static string GroupNameForDay(int eventDayId) => $"day-{eventDayId}";

    public Task JoinDayGroup(int eventDayId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForDay(eventDayId));

    public Task LeaveDayGroup(int eventDayId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameForDay(eventDayId));
}
