using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Domain.Models;

public class BrandSalesSummary
{
    public Brand Brand { get; set; }
    public List<Sale> Sales { get; set; } = [];
    public decimal TotalPrice { get; set; }
    public decimal TotalDiscount { get; set; }
    public int TotalQuantityUnits { get; set; }
    public int SalesCount { get; set; }
    public decimal NetTotal => TotalPrice - TotalDiscount;
}
