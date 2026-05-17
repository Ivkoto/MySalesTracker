using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Domain.Entities;

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

    public Currency Currency {get; init; } = Currency.EUR;

    public string? Notes { get; init; }
}
