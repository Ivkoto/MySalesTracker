using Microsoft.EntityFrameworkCore;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Infrastructure.Persistence.Repositories;

internal class ProductRepository(IDbContextFactory<AppDbContext> contextFactory) : IProductRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory = contextFactory;

    public async Task<List<Product>> GetActiveProductsAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Brand)
                .ToListAsync(ct);
    }

    public async Task<List<PriceRule>> GetPriceRulesForProductAsync(int productId, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.PriceRules
                .Where(r => r.ProductId == productId)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);
    }
}
