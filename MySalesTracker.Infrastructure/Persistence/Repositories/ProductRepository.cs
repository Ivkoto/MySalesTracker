using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Infrastructure.Persistence.Repositories;

internal class ProductRepository(IDbContextFactory<AppDbContext> contextFactory) : IProductRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory = contextFactory;

    public async Task<bool> TryAddAsync(Product product, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var exists = await context.Products.AnyAsync(p => p.IsActive && p.Brand == product.Brand && p.Name == product.Name, ct);
        
        if (exists)
        {
            return false;
        }

        try
        {
            context.Products.Add(product);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsActiveBrandNameViolation(ex))
        {
            return false;
        }
    }

    // Only the active-product-name index means "duplicate product". Other failures, deadlock 1205 included, must surface.
    private static bool IsActiveBrandNameViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 2601 or 2627 } sqlEx
            && sqlEx.Message.Contains("UX_Products_Active_Brand_Name", StringComparison.Ordinal);

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
