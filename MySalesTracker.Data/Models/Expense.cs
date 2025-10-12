using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySalesTracker.Data.Models;

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
