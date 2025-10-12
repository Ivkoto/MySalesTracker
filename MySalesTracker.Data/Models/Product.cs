using System.ComponentModel.DataAnnotations;

namespace MySalesTracker.Data.Models;

public class Product
{
    [Key]
    public int ProductId { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    public Brand Brand { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<PriceRule> PriceRules { get; set; } = [];
}
