using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Application.Interfaces;
public interface IProductRepository
{
    Task<List<Product>> GetActiveProductsAsync(CancellationToken ct);
    Task<List<Product>> GetActiveProductsWithPriceRulesAsync(CancellationToken ct);
    Task<Product?> GetActiveProductWithPriceRulesAsync(int productId, CancellationToken ct);
}
