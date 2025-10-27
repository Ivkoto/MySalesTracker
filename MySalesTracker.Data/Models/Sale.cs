using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySalesTracker.Data.Models;

public class Sale
{
    [Key]
    public int SaleId { get; set; }

    [ForeignKey("EventDay")]
    public int EventDayId { get; set; }
    public EventDay EventDay { get; set; } = null!;

    [ForeignKey("Product")]
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [ForeignKey("PriceRule")]
    public int? PriceRuleId { get; set; }

    public PriceRule? PriceRule { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal Price { get; set; }

    public int QuantityUnits { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal DiscountValue { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
