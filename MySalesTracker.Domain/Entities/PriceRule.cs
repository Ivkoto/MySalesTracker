using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySalesTracker.Domain.Entities;

public sealed class PriceRule
{
    [Key]
    public int PriceRuleId { get; init; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal Price { get; init; }

    public int UnitsPerSale { get; init; }

    [ForeignKey("Product")]
    public int ProductId { get; init; }

    public int SortOrder { get; init; }

    public Product Product { get; init; } = null!;

    [Column(TypeName = "date")]
    public DateOnly EffectiveFrom { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Column(TypeName = "date")]
    public DateOnly? EffectiveTo { get; init; }
}
