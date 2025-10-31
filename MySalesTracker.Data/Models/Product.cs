using System.ComponentModel.DataAnnotations;

namespace MySalesTracker.Data.Models;

public sealed class Product
{
    [Key]
    public int ProductId { get; init; }

    [StringLength(50)]
    public string Name { get; init; } = null!;

    public Brand Brand { get; init; }

    public bool IsActive { get; set; } = true;

    public ICollection<PriceRule> PriceRules { get; init; } = [];
}
