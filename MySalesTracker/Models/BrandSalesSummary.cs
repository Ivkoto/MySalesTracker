using MySalesTracker.Data;
using MySalesTracker.Data.Models;

namespace MySalesTracker.Models;

/// <summary>
/// Summary of sales for a specific brand.
/// </summary>
public class BrandSalesSummary
{
    /// <summary>
    /// The brand for this summary.
    /// </summary>
    public Brand Brand { get; set; }

    /// <summary>
    /// Individual sale transactions (loaded separately for display).
    /// </summary>
    public List<Sale> Sales { get; set; } = [];
    
    /// <summary>
    /// Total of all unit prices (before discount).
    /// </summary>
    public decimal TotalPrice { get; set; }
    
    /// <summary>
    /// Total of all discounts applied.
    /// </summary>
    public decimal TotalDiscount { get; set; }

    /// <summary>
    /// Total quantity of units sold (sum of all QuantityUnits).
    /// </summary>
    public int TotalQuantityUnits { get; set; }
   
    /// <summary>
    /// Number of sale transactions in this brand.
    /// </summary>
    public int SalesCount {get; set; }
    
    /// <summary>
    /// Final total after discounts (net amount).
    /// </summary>
    public decimal NetTotal => TotalPrice - TotalDiscount;
}
