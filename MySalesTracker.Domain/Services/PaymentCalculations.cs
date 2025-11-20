using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Domain.Services;

/// <summary>
/// Pure calculation functions for payment tracking and reconciliation.
/// Contains no state or dependencies—testable in isolation.
/// </summary>
public static class PaymentCalculations
{
    /// <summary>
    /// Calculates the total amount across all payments.
    /// </summary>
    /// <param name="payments">Collection of payments to sum.</param>
    /// <returns>Total amount of all payments.</returns>
    public static decimal CalculateTotalPayments(IEnumerable<Payment> payments)
        => payments.Sum(p => p.Amount);

    /// <summary>
    /// Calculates the difference between total payments and total sales.
    /// </summary>
    /// <param name="totalPayments">Total amount of all payments.</param>
    /// <param name="totalSales">Total amount of all sales.</param>
    /// <returns>Difference (positive means more payments than sales, negative means less).</returns>
    public static decimal CalculatePaymentDifference(decimal totalPayments, decimal totalSales)
        => totalPayments - totalSales;

    /// <summary>
    /// Groups payments by payment method for easy lookup.
    /// </summary>
    /// <param name="payments">Collection of payments to group.</param>
    /// <returns>Dictionary keyed by payment method with the payment amount.</returns>
    public static Dictionary<PaymentMethod, decimal> GroupPaymentsByMethod(IEnumerable<Payment> payments)
        => payments
            .GroupBy(p => p.Method)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

    /// <summary>
    /// Gets the payment amount for a specific payment method, or zero if not found.
    /// </summary>
    /// <param name="payments">Collection of payments to search.</param>
    /// <param name="method">The payment method to find.</param>
    /// <returns>Amount for the specified method, or 0 if not found.</returns>
    public static decimal GetAmountForMethod(IEnumerable<Payment> payments, PaymentMethod method)
        => payments.Where(p => p.Method == method).Sum(p => p.Amount);
}

