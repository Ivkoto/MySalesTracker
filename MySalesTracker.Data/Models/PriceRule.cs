using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySalesTracker.Data.Models;

public class PriceRule
{
    [Key]
    public int PriceRuleId { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal Price { get; set; }

    public int UnitsPerSale { get; set; }

    [ForeignKey("Product")]
    public int ProductId { get; set; }

    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Column(TypeName = "date")]
    public DateOnly? EffectiveTo { get; set; }
}
