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

    [ForeignKey("Customer")]
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [ForeignKey("PriceRule")]
    public int? PriceRuleId { get; set; }
    public PriceRule? PriceRule { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal UnitPrice { get; set; }
    public int QuantityUnits { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal DiscountValue { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
public class Expense
{
    [Key]
    public int ExpenseId { get; set; }

    [ForeignKey("EventDay")]
    public int EventDayId { get; set; }
    public EventDay EventDay { get; set; } = null!;

    [StringLength(200)]
    public string Type { get; set; } = null!;

    [Column(TypeName = "decimal(6,2)")]
    public decimal Amount { get; set; }

    public string? Notes { get; set; }
}
public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    [ForeignKey("EventDay")]
    public int EventDayId { get; set; }
    public EventDay EventDay { get; set; } = null!;

    public PaymentMethod Method { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal Amount { get; set; }
}
