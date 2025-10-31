using Microsoft.EntityFrameworkCore;
using MySalesTracker.Data;
using MySalesTracker.Data.Models;

namespace MySalesTracker.Services;

public sealed class ProductService(IDbContextFactory<AppDbContext> contextFactory, ILogger<ProductService> logger)
{
    /// <summary>
    /// Gets all active products ordered by brands.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of active <see cref="Product"/> objects (where IsActive = true),
    /// ordered by <see cref="Brand"/> descending. Returns an empty list if no active products exist.
    /// </returns>
    public async Task<List<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Brand)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve active products");
            throw;
        }
    }

    /// <summary>
    /// Gets price rules for a specific product.
    /// </summary>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="PriceRule"/> objects associated with the specified product,
    /// ordered by <see cref="PriceRule.SortOrder"/> ascending for display consistency.
    /// Returns an empty list if the product has no price rules.
    /// </returns>
    public async Task<List<PriceRule>> GetPriceRulesForProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.PriceRules
                .Where(r => r.ProductId == productId)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve price rules for Product {ProductId}", productId);
            throw;
        }
    }
}
