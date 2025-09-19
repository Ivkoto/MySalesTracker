namespace MySalesTracker.Data.Models;

public class Sale
{
    public int SaleId { get; set; }
    public int EventDayId { get; set; }
    public EventDay EventDay { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? PriceRuleId { get; set; }       // when chosen from dropdown
    public PriceRule? PriceRule { get; set; }
    public decimal UnitPrice { get; set; }      // snapshot
    public int QuantityUnits { get; set; }      // snapshot; auto from rul
    public decimal DiscountValue { get; set; }  // amount or percent
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
public class Expense
{
    public int ExpenseId { get; set; }
    public int EventDayId { get; set; }
    public EventDay EventDay { get; set; } = null!;
    public string Type { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}
public class Payment
{
    public int PaymentId { get; set; }
    public int EventDayId { get; set; }
    public EventDay EventDay { get; set; } = null!;
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
}
