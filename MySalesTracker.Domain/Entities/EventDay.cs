using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySalesTracker.Domain.Entities;

public sealed class EventDay
{
    [Key]
    public int EventDayId { get; init; }

    [ForeignKey("Event")]
    public int EventId { get; init; }

    public Event Event { get; init; } = null!;

    [Column(TypeName = "date")]
    public required DateOnly Date { get; init; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal? StartingPettyCash { get; set; }

    public ICollection<Sale> Sales { get; init; } = [];
    public ICollection<Expense> Expenses { get; init; } = [];
    public ICollection<Payment> PaymentsCounted { get; init; } = [];
}
