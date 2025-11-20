using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Application.Interfaces;

public interface IPaymentRepository
{
    /// <summary>
    /// Retrieves all payment records for a specific event day.
    /// </summary>
    /// <param name="eventDayId">The ID of the event day.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of payments for the event day.</returns>
    Task<List<Payment>> GetPaymentsByEventDayAsync(int eventDayId, CancellationToken ct = default);

    /// <summary>
    /// Inserts a new payment or updates an existing one based on EventDayId and Method.
    /// </summary>
    /// <param name="eventDayId">The ID of the event day.</param>
    /// <param name="method">The payment method.</param>
    /// <param name="amount">The payment amount.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved payment record.</returns>
    Task<Payment> UpsertPaymentAsync(int eventDayId, PaymentMethod method, decimal amount, CancellationToken ct = default);

    /// <summary>
    /// Deletes a payment record by its ID.
    /// </summary>
    /// <param name="paymentId">The ID of the payment to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeletePaymentAsync(int paymentId, CancellationToken ct = default);
}

