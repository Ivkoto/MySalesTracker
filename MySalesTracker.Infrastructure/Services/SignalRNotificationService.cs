using Microsoft.AspNetCore.SignalR;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Infrastructure.Hubs;

namespace MySalesTracker.Infrastructure.Services;

/// <summary>
/// SignalR implementation of notification service.
/// </summary>
internal sealed class SignalRNotificationService(IHubContext<SalesHub> hubContext) : INotificationService
{
    public async Task NotifySaleCreatedAsync(int eventDayId, int saleId, CancellationToken ct = default)
    {
        await hubContext.Clients
            .Group(SalesHub.GroupNameForDay(eventDayId))
            .SendAsync("SaleCreated", eventDayId, saleId, ct);
    }
}
