namespace MySalesTracker.Application.Interfaces;

/// <summary>
/// Service for sending real-time notifications to clients.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Notifies clients that a new sale has been created.
    /// </summary>
    /// <param name="eventDayId">The ID of the event day.</param>
    /// <param name="saleId">The ID of the newly created sale.</param>
    /// <param name="ct">Cancellation token.</param>
    Task NotifySaleCreatedAsync(int eventDayId, int saleId, CancellationToken ct = default);
}
