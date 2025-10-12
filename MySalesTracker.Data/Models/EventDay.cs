using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySalesTracker.Data.Models;

public class EventDay
{
    [Key]
    public int EventDayId { get; set; }

    [ForeignKey("Event")]
    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateOnly Date { get; set; }

    public ICollection<Sale> Sales { get; set; } = [];
    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<Payment> PaymentsCounted { get; set; } = [];
}
