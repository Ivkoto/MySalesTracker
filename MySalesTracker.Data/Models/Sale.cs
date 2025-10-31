using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySalesTracker.Data.Models;

public sealed class Sale
{
    [Key]
    public int SaleId { get; init; }

    [ForeignKey("EventDay")]
    public int EventDayId { get; init; }
    public EventDay EventDay { get; init; } = null!;

    [ForeignKey("Product")]
    public int ProductId { get; init; }
    public Product Product { get; init; } = null!;

    [ForeignKey("PriceRule")]
    public int? PriceRuleId { get; init; }
    public PriceRule? PriceRule { get; init; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal Price { get; init; }

    public int QuantityUnits { get; init; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal DiscountValue { get; init; }

    public string? Notes { get; init; }

    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
}
