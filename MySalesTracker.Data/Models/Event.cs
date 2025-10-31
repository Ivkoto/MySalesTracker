using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySalesTracker.Data.Models;

public sealed class Event
{
    [Key]
    public int EventId { get; init; }

    [StringLength(50)]
    public string Name { get; init; } = null!;

    [Column(TypeName = "date")]
    public DateOnly StartDate { get; init; }

    [Column(TypeName = "date")]
    public DateOnly EndDate { get; init; }

    public ICollection<EventDay> Days { get; init; } = [];
}

