using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Application.Interfaces;
public interface IProductRepository
{
    Task<List<Product>> GetActiveProductsAsync(CancellationToken ct);
}
