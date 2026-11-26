using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Infrastructure.Hubs;

namespace MySalesTracker.Infrastructure.Services;

/// <summary>
/// SignalR implementation of notification service.
/// </summary>
internal sealed class SignalRNotificationService(IHubContext<SalesHub> hubContext, ILogger<SignalRNotificationService> logger) : INotificationService
{
    public async Task NotifySaleCreatedAsync(int eventDayId, int saleId, CancellationToken ct = default)
    {
        await hubContext.Clients
            .Group(SalesHub.GroupNameForDay(eventDayId))
            .SendAsync("SaleCreated", eventDayId, saleId, ct);
    }

    public async Task NotifyEventCreatedAsync(int eventId, CancellationToken ct = default)
    {
        //TODO: Implement along with Events.razor(132-136)
    }

    public async Task NotifyProductCreatedAsync(CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients.All.SendAsync("ProductCreated", ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Failed to notify clients about the new product.");
        }
    }
}
