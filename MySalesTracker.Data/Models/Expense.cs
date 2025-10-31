using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySalesTracker.Data.Models;

public sealed class Expense
{
    [Key]
    public int ExpenseId { get; init; }

    [ForeignKey("EventDay")]
    public int EventDayId { get; init; }
    public EventDay EventDay { get; init; } = null!;

    [StringLength(200)]
    public string Type { get; init; } = null!;

    [Column(TypeName = "decimal(6,2)")]
    public decimal Amount { get; init; }

    public string? Notes { get; init; }
}
