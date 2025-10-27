using Microsoft.EntityFrameworkCore;
using MySalesTracker.Data;
using MySalesTracker.Data.Models;

namespace MySalesTracker.Services;

public class ProductService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IDbContextFactory<AppDbContext> contextFactory, ILogger<ProductService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets all active products ordered by brands.
    /// </summary>
    public async Task<List<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Brand)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active products");
            throw;
        }
    }

    /// <summary>
    /// Gets price rules for a specific product.
    /// </summary>
    public async Task<List<PriceRule>> GetPriceRulesForProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.PriceRules
                .Where(r => r.ProductId == productId)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve price rules for Product {ProductId}", productId);
            throw;
        }
    }
}
