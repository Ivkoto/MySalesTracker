using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Application.Interfaces;

/// <summary>
/// Repository for adding products and retrieving active catalog entries and their price rules.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Attempts to add a product and returns <see langword="false"/> if an active product with the same brand and name exists.
    /// </summary>
    Task<bool> TryAddAsync(Product product, CancellationToken ct);

    /// <summary>
    /// Retrieves the active products in the catalog.
    /// </summary>
    Task<List<Product>> GetActiveProductsAsync(CancellationToken ct);

    /// <summary>
    /// Retrieves the active products together with their configured price rules.
    /// </summary>
    Task<List<Product>> GetActiveProductsWithPriceRulesAsync(CancellationToken ct);

    /// <summary>
    /// Finds an active product by ID and includes its price rules.
    /// </summary>
    Task<Product?> GetActiveProductWithPriceRulesAsync(int productId, CancellationToken ct);
}
