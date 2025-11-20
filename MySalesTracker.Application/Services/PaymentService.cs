using Microsoft.Extensions.Logging;
using MySalesTracker.Application.DTOs;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;
using MySalesTracker.Domain.Models;
using MySalesTracker.Domain.Services;

namespace MySalesTracker.Application.Services;

public sealed class PaymentService(
    IPaymentRepository paymentRepository,
    ISaleRepository saleRepository,
    ILogger<PaymentService> logger)
{
    /// <summary>
    /// Gets all payments for a specific event day.
    /// </summary>
    /// <param name="eventDayId">The ID of the event day.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of payments for the event day.</returns>
    public async Task<List<Payment>> GetPaymentsByEventDayAsync(int eventDayId, CancellationToken ct = default)
    {
        try
        {
            return await paymentRepository.GetPaymentsByEventDayAsync(eventDayId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve payments for EventDay {EventDayId}", eventDayId);
            throw;
        }
    }

    /// <summary>
    /// Saves a payment for a specific event day and payment method.
    /// If a payment already exists for the method, it will be updated; otherwise, a new payment is created.
    /// </summary>
    /// <param name="eventDayId">The ID of the event day.</param>
    /// <param name="method">The payment method.</param>
    /// <param name="amount">The payment amount.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The saved payment record.</returns>
    public async Task<Payment> SavePaymentAsync(int eventDayId, PaymentMethod method, decimal amount, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation(
                "Saving payment for EventDay {EventDayId}, Method {Method}, Amount {Amount}",
                eventDayId, method, amount);

            var payment = await paymentRepository.UpsertPaymentAsync(eventDayId, method, amount, ct);

            logger.LogInformation("Successfully saved payment {PaymentId}", payment.PaymentId);
            return payment;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save payment for EventDay {EventDayId}", eventDayId);
            throw;
        }
    }

    /// <summary>
    /// Gets a comprehensive payment summary for an event day, including payments, sales totals, and reconciliation.
    /// </summary>
    /// <param name="eventDayId">The ID of the event day.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ServiceResult{PaymentSummary}"/> representing the operation result. On success
    /// the result contains the aggregated <see cref="PaymentSummary"/>; on failure it contains
    /// an error message describing the problem.
    /// </returns>
    public async Task<ServiceResult<PaymentSummary>> GetPaymentSummaryAsync(int eventDayId, CancellationToken ct = default)
    {
        try
        {
            var payments = await paymentRepository.GetPaymentsByEventDayAsync(eventDayId, ct);
            var sales = await saleRepository.GetSalesByEventDay(eventDayId, ct);

            var brandSummaries = SalesCalculations.GroupSalesByBrand(sales);
            var paymentsByMethod = PaymentCalculations.GroupPaymentsByMethod(payments);

            var totalPayments = PaymentCalculations.CalculateTotalPayments(payments);
            var totalSales = SalesCalculations.CalculateNetRevenue(sales);
            var difference = PaymentCalculations.CalculatePaymentDifference(totalPayments, totalSales);

            logger.LogInformation(
                "Payment summary for EventDay {EventDayId}: Payments={TotalPayments}, Sales={TotalSales}, Difference={Difference}",
                eventDayId, totalPayments, totalSales, difference);

            var summary = new PaymentSummary
            {
                Payments = paymentsByMethod,
                BrandSalesTotals = brandSummaries.ToDictionary(b => b.Brand, b => b.NetTotal),
                TotalPayments = totalPayments,
                TotalSales = totalSales,
                Difference = difference
            };

            return ServiceResult<PaymentSummary>.SuccessResult(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get payment summary for EventDay {EventDayId}", eventDayId);
            return ServiceResult<PaymentSummary>.FailureResult($"Грешка при зареждане на данни: {ex.Message}");
        }
    }
}

