using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Application.Interfaces;
public interface IProductRepository
{
    Task<List<Product>> GetActiveProducts(CancellationToken ct);

    Task<List<PriceRule>> GetPriceRulesForProduct(int productId, CancellationToken ct);
}
