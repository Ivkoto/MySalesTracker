using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Domain.Entities;

public sealed class Payment
{
    [Key]
    public int PaymentId { get; set; }

    [ForeignKey("EventDay")]
    public int EventDayId { get; set; }
    public EventDay EventDay { get; set; } = null!;

    public PaymentMethod Method { get; set; }

    //TODO: Scale up the precision to 8,2 or 10,2 since now the max amount could be 9999.99
    [Column(TypeName = "decimal(6,2)")]
    public decimal Amount { get; set; }
}
