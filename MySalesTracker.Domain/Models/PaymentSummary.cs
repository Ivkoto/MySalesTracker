using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Domain.Models;

public sealed class PaymentSummary
{
    public required Dictionary<PaymentMethod, decimal> Payments { get; init; }
    public required Dictionary<Brand, decimal> BrandSalesTotals { get; init; }
    public required decimal TotalPayments { get; init; }
    public required decimal TotalSales { get; init; }
    public required decimal Difference { get; init; }
}
