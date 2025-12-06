using Microsoft.EntityFrameworkCore;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Infrastructure.Persistence.Repositories;
internal class PriceRuleRepository(IDbContextFactory<AppDbContext> contextFactory) : IPriceRuleRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory = contextFactory;

    public async Task<List<PriceRule>> GetAllPriceRulesAsync(DateOnly onDate, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var result =  await context.PriceRules
            .Where (r => r.EffectiveFrom <= onDate && (r.EffectiveTo == null || r.EffectiveTo >= onDate))
            .ToListAsync(ct);

        return result;
    }

    public async Task<PriceRule?> GetUnitsForProductAsync(int productId, decimal price, DateOnly onDate, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var priceRule = await context.PriceRules
                .Where(r => r.ProductId == productId
                    && r.Price == price
                    && r.EffectiveFrom <= onDate
                    && (r.EffectiveTo == null || r.EffectiveTo >= onDate))
                .OrderByDescending(r => r.EffectiveFrom)
                .ThenBy(r => r.SortOrder)
                .FirstOrDefaultAsync(ct);

        return priceRule;
    }
}
