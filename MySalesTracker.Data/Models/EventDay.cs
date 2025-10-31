using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySalesTracker.Data.Models;

public sealed class EventDay
{
    [Key]
    public int EventDayId { get; init; }

    [ForeignKey("Event")]
    public int EventId { get; init; }

    public Event Event { get; init; } = null!;

    [Column(TypeName = "date")]
    public DateOnly Date { get; init; }

    public ICollection<Sale> Sales { get; init; } = [];
    public ICollection<Expense> Expenses { get; init; } = [];
    public ICollection<Payment> PaymentsCounted { get; init; } = [];
}
