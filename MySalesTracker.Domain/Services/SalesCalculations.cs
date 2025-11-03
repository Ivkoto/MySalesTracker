using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;
using MySalesTracker.Domain.Models;

namespace MySalesTracker.Domain.Services;

/// <summary>
/// Pure calculation functions for sales domain logic.
/// Contains no state or dependencies—testable in isolation.
/// </summary>
public static class SalesCalculations
{
    /// <summary>
    /// Calculates the net total for a single sale (price minus discount).
    /// </summary>
    /// <param name="price">The sale price.</param>
    /// <param name="discount">The discount amount.</param>
    /// <returns>Net amount after discount.</returns>
    public static decimal CalculateNetAmount(decimal price, decimal discount)
        => price - discount;

    /// <summary>
    /// Groups sales by brand and creates aggregated summaries.
    /// </summary>
    /// <param name="sales">Collection of sales to summarize.</param>
    /// <returns>List of brand summaries with aggregated totals.</returns>
    public static List<BrandSalesSummary> GroupSalesByBrand(IEnumerable<Sale> sales)
        => sales.GroupBy(s => s.Product.Brand).Select(CreateBrandSummary).ToList();

    /// <summary>
    /// Creates a brand summary from a grouped collection of sales.
    /// </summary>
    private static BrandSalesSummary CreateBrandSummary(IGrouping<Brand, Sale> brandGroup)
        => new()
        {
            Brand = brandGroup.Key,
            TotalPrice = brandGroup.Sum(s => s.Price),
            TotalDiscount = brandGroup.Sum(s => s.DiscountValue),
            TotalQuantityUnits = brandGroup.Sum(s => s.QuantityUnits),
            SalesCount = brandGroup.Count(),
            Sales = brandGroup.OrderByDescending(s => s.SaleId).ToList()
        };

    /// <summary>
    /// Validates sale input parameters.
    /// </summary>
    /// <param name="unitPrice">The unit price to validate.</param>
    /// <param name="quantityUnits">The quantity to validate.</param>
    /// <returns>Validation result with error message if invalid.</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateSaleInput(decimal unitPrice, int quantityUnits)
    {
        if (unitPrice <= 0)
            return (false, "Unit price must be greater than zero");

        if (quantityUnits <= 0)
            return (false, "Quantity units must be greater than zero");

        return (true, null);
    }

    /// <summary>
    /// Calculates the total revenue from a collection of sales.
    /// </summary>
    /// <param name="sales">Collection of sales.</param>
    /// <returns>Total revenue (sum of all prices).</returns>
    public static decimal CalculateTotalRevenue(IEnumerable<Sale> sales)
        => sales.Sum(s => s.Price);

    /// <summary>
    /// Calculates the total discounts given across all sales.
    /// </summary>
    /// <param name="sales">Collection of sales.</param>
    /// <returns>Total discounts given.</returns>
    public static decimal CalculateTotalDiscounts(IEnumerable<Sale> sales)
        => sales.Sum(s => s.DiscountValue);

    /// <summary>
    /// Calculates net revenue (revenue minus discounts).
    /// </summary>
    /// <param name="sales">Collection of sales.</param>
    /// <returns>Net revenue after discounts.</returns>
    public static decimal CalculateNetRevenue(IEnumerable<Sale> sales)
        => CalculateTotalRevenue(sales) - CalculateTotalDiscounts(sales);
}
