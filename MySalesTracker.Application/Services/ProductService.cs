using Microsoft.Extensions.Logging;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Application.Services;

public sealed class ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
{
    /// <summary>
    /// Gets all active products ordered by brands.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of active <see cref="Product"/> objects (where IsActive = true),
    /// ordered by <see cref="Brand"/> descending. Returns an empty list if no active products exist.
    /// </returns>
    public async Task<List<Product>> GetActiveProductsAsync(CancellationToken ct = default)
    {
        try
        {
            return await productRepository.GetActiveProducts(ct);
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
    public async Task<List<PriceRule>> GetPriceRulesForProductAsync(int productId, CancellationToken ct = default)
    {
        try
        {
            return await productRepository.GetPriceRulesForProduct(productId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve price rules for Product {ProductId}", productId);
            throw;
        }
    }
}
