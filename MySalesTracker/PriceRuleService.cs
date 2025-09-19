using Microsoft.EntityFrameworkCore;
using MySalesTracker.Data;

namespace MySalesTracker;

public class PriceRuleService
{
    private readonly AppDbContext _context;
    public PriceRuleService(AppDbContext db) => _context = db;
    public async Task<int> GetUnitsForAsync(int productId, decimal price, DateOnly onDate)
        => await _context.PriceRules
                .Where(r => r.ProductId == productId
                    && r.Price == price
                    && r.EffectiveFrom <= onDate
                    && (r.EffectiveTo == null || r.EffectiveTo >= onDate))
                .OrderBy(r => r.SortOrder)
                .Select(r => r.UnitsPerSale)
                .FirstOrDefaultAsync() switch { 0 => 1, var u => u };
}
