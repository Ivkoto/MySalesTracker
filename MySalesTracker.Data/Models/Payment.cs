using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySalesTracker.Data.Models;

public sealed class Payment
{
    [Key]
    public int PaymentId { get; init; }

    [ForeignKey("EventDay")]
    public int EventDayId { get; init; }
    public EventDay EventDay { get; init; } = null!;

    public PaymentMethod Method { get; init; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal Amount { get; init; }
}
