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
            return await productRepository.GetActiveProductsAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve active products");
            throw;
        }
    }
}
