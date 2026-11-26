using System.ComponentModel.DataAnnotations;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Domain.Entities;

public sealed class Product
{
    public const int MaxNameLength = 50;

    [Key]
    public int ProductId { get; init; }

    [StringLength(MaxNameLength)]
    public required string Name { get; init; }

    public Brand Brand { get; init; }

    public bool IsActive { get; set; } = true;

    public ICollection<PriceRule> PriceRules { get; init; } = [];
}
