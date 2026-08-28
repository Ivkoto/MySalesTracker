using System.Data;
using Microsoft.EntityFrameworkCore;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Infrastructure.Persistence.Repositories;
internal class PriceRuleRepository(IDbContextFactory<AppDbContext> contextFactory) : IPriceRuleRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory = contextFactory;

    public async Task<List<PriceRule>> GetAllPriceRulesAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.PriceRules
            .AsNoTracking()
            .OrderBy(r => r.ProductId)
            .ThenBy(r => r.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<PriceRule?> GetRuleForProductAsync(int productId, decimal price, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.PriceRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.Price == price, ct);
    }

    public async Task ReplacePriceRulesAsync(
        int productId,
        IReadOnlyCollection<PriceRule> priceRules,
        CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var existingRules = await context.PriceRules
            .Where(r => r.ProductId == productId)
            .ToListAsync(ct);

        if (existingRules.Count > 0)
        {
            context.PriceRules.RemoveRange(existingRules);
            await context.SaveChangesAsync(ct);
        }

        context.PriceRules.AddRange(priceRules);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
