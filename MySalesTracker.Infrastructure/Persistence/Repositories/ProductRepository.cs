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
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Brand)
                .ToListAsync(ct);
    }

    public async Task<List<Product>> GetActiveProductsWithPriceRulesAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.PriceRules)
            .OrderBy(p => p.Brand)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<Product?> GetActiveProductWithPriceRulesAsync(int productId, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.ProductId == productId)
            .Include(p => p.PriceRules)
            .SingleOrDefaultAsync(ct);
    }
}
