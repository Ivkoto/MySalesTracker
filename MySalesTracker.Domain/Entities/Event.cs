using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Domain.Entities;

public sealed class Event
{
    [Key]
    public int EventId { get; init; }

    [StringLength(50)]
    public required string Name { get; init; }

    [Column(TypeName = "date")]
    public required DateOnly StartDate { get; init; }

    [Column(TypeName = "date")]
    public required DateOnly EndDate { get; init; }

    public PriceList PriceList { get; init; } = PriceList.BG;

    public ICollection<EventDay> Days { get; init; } = [];
}

