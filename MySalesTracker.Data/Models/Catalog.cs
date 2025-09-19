namespace MySalesTracker.Data.Models;

public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public Brand Brand { get; set; } // enum: Totem, Ceramics, Candles (use Display for labels "ТОТЕМ", "Керамика", "Свещи")
    public bool IsActive { get; set; } = true;
    public ICollection<PriceRule> PriceRules { get; set; } = [];
}

public class PriceRule
{
    public int PriceRuleId { get; set; }
    public decimal Price { get; set; }          // 19, 29, 38, 57 …
    public int UnitsPerSale { get; set; }       // 1, 2, 3 …
    public int ProductId { get; set; }
    public int SortOrder { get; set; }
    public Product Product { get; set; } = null!;
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EffectiveTo { get; set; }
}
