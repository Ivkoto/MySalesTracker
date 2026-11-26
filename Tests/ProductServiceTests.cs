using Microsoft.Extensions.Logging.Abstractions;
using MySalesTracker.Application.Services;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Tests;

public sealed class ProductServiceTests
{
    [Theory]
    [InlineData(Brand.Totem, "  ")]
    [InlineData((Brand)0, "Нов продукт")]
    public async Task CreateProductAsync_InvalidInput_DoesNotSave(Brand brand, string name)
    {
        var repository = new ProductRepositoryFake();
        var products = new ProductService(repository, NullLogger<ProductService>.Instance);
        var ct = TestContext.Current.CancellationToken;

        var result = await products.CreateProductAsync(brand, name, ct);

        Assert.False(result.Success);
        Assert.Empty(await products.GetActiveProductsAsync(ct));
    }

    [Fact]
    public async Task CreateProductAsync_DuplicateName_ReturnsFailureAndKeepsStoredProducts()
    {
        var existing = new Product { ProductId = 1, Name = "Свещ", Brand = Brand.Candles };
        var repository = new ProductRepositoryFake(existing) { AcceptAdds = false };
        var products = new ProductService(repository, NullLogger<ProductService>.Instance);
        var ct = TestContext.Current.CancellationToken;

        var result = await products.CreateProductAsync(Brand.Candles, "Свещ", ct);

        Assert.False(result.Success);
        Assert.Equal("В тази марка вече има продукт с това име.", result.ErrorMessage);
        var stored = Assert.Single(await products.GetActiveProductsAsync(ct));
        Assert.Same(existing, stored);
    }
}
