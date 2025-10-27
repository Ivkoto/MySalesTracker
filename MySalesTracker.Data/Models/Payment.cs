using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySalesTracker.Data.Models;

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
