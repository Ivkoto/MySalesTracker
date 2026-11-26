using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Tests;

internal sealed class ProductRepositoryFake(params Product[] initialProducts) : IProductRepository
{
    private readonly List<Product> products = [.. initialProducts];

    public bool AcceptAdds { get; init; } = true;

    public Task<bool> TryAddAsync(Product newProduct, CancellationToken ct)
    {
        if (!AcceptAdds)
        {
            return Task.FromResult(false);
        }

        products.Add(newProduct);
        return Task.FromResult(true);
    }

    public Task<List<Product>> GetActiveProductsAsync(CancellationToken ct)
        => Task.FromResult(products.Where(product => product.IsActive).ToList());

    public Task<List<Product>> GetActiveProductsWithPriceRulesAsync(CancellationToken ct)
        => GetActiveProductsAsync(ct);

    public Task<Product?> GetActiveProductWithPriceRulesAsync(int productId, CancellationToken ct)
        => Task.FromResult(products.FirstOrDefault(product => product.IsActive && product.ProductId == productId));
}
