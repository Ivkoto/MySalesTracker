using Microsoft.Extensions.Logging.Abstractions;
using MySalesTracker.Application.DTOs;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Application.Services;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Tests;

public sealed class PriceRuleServiceTests
{
    [Fact]
    public async Task UpdateProductPriceRulesAsync_ValidRules_CreatesOrderedReplacementSet()
    {
        var priceRuleRepository = new PriceRuleRepositoryFake();
        var productRepository = new ProductRepositoryFake(CreateProduct(Brand.Totem));
        var service = CreateService(priceRuleRepository, productRepository);
        var request = new UpdateProductPriceRulesRequest(
            1,
            [new PriceRuleInput(40m, 1, false), new PriceRuleInput(75m, 2, true)]);

        var result = await service.UpdateProductPriceRulesAsync(request);

        Assert.True(result.Success);
        Assert.Equal(1, priceRuleRepository.SavedProductId);
        Assert.Collection(
            priceRuleRepository.SavedRules,
            first =>
            {
                Assert.Equal(40m, first.Price);
                Assert.Equal(1, first.SortOrder);
                Assert.False(first.IsDefault);
            },
            second =>
            {
                Assert.Equal(75m, second.Price);
                Assert.Equal(2, second.SortOrder);
                Assert.True(second.IsDefault);
            });
    }

    [Fact]
    public async Task UpdateProductPriceRulesAsync_DuplicatePrices_ReturnsFailure()
    {
        var priceRuleRepository = new PriceRuleRepositoryFake();
        var service = CreateService(priceRuleRepository, new ProductRepositoryFake(CreateProduct(Brand.Totem)));
        var request = new UpdateProductPriceRulesRequest(
            1,
            [new PriceRuleInput(40m, 1, true), new PriceRuleInput(40m, 2, false)]);

        var result = await service.UpdateProductPriceRulesAsync(request);

        Assert.False(result.Success);
        Assert.Empty(priceRuleRepository.SavedRules);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task UpdateProductPriceRulesAsync_InvalidDefaultCount_ReturnsFailure(bool firstDefault, bool secondDefault)
    {
        var priceRuleRepository = new PriceRuleRepositoryFake();
        var service = CreateService(priceRuleRepository, new ProductRepositoryFake(CreateProduct(Brand.Totem)));
        var request = new UpdateProductPriceRulesRequest(
            1,
            [new PriceRuleInput(40m, 1, firstDefault), new PriceRuleInput(75m, 2, secondDefault)]);

        var result = await service.UpdateProductPriceRulesAsync(request);

        Assert.False(result.Success);
        Assert.Empty(priceRuleRepository.SavedRules);
    }

    [Fact]
    public async Task UpdateProductPriceRulesAsync_CeramicsProduct_ReturnsFailure()
    {
        var priceRuleRepository = new PriceRuleRepositoryFake();
        var service = CreateService(priceRuleRepository, new ProductRepositoryFake(CreateProduct(Brand.Ceramics)));
        var request = new UpdateProductPriceRulesRequest(
            1,
            [new PriceRuleInput(40m, 1, true)]);

        var result = await service.UpdateProductPriceRulesAsync(request);

        Assert.False(result.Success);
        Assert.Empty(priceRuleRepository.SavedRules);
    }

    [Fact]
    public async Task GetProductPricingAsync_ReturnsCurrentRulesInConfiguredOrder()
    {
        var product = CreateProduct(
            Brand.Candles,
            CreateRule(20m, false, 2),
            CreateRule(10m, true, 1));
        var service = CreateService(new PriceRuleRepositoryFake(), new ProductRepositoryFake(product));

        var result = await service.GetProductPricingAsync();

        var pricing = Assert.Single(result);
        Assert.False(pricing.IsFreePrice);
        Assert.Collection(
            pricing.CurrentRules,
            first =>
            {
                Assert.Equal(10m, first.Price);
                Assert.True(first.IsDefault);
            },
            second => Assert.Equal(20m, second.Price));
    }

    [Fact]
    public async Task GetUnitsForProductAsync_ConfiguredPrice_ReturnsUnits()
    {
        var repository = new PriceRuleRepositoryFake
        {
            RuleToReturn = CreateRule(30m, true, 1, unitsPerSale: 3)
        };
        var service = CreateService(repository, new ProductRepositoryFake(CreateProduct(Brand.Candles)));

        var result = await service.GetUnitsForProductAsync(1, 30m);

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task GetUnitsForProductAsync_MissingPrice_ReturnsNull()
    {
        var service = CreateService(
            new PriceRuleRepositoryFake(),
            new ProductRepositoryFake(CreateProduct(Brand.Candles)));

        var result = await service.GetUnitsForProductAsync(1, 30m);

        Assert.Null(result);
    }

    private static PriceRuleService CreateService(
        IPriceRuleRepository priceRuleRepository,
        IProductRepository productRepository)
        => new(priceRuleRepository, productRepository, NullLogger<PriceRuleService>.Instance);

    private static Product CreateProduct(Brand brand, params PriceRule[] rules)
        => new()
        {
            ProductId = 1,
            Name = "Test product",
            Brand = brand,
            PriceRules = rules.ToList()
        };

    private static PriceRule CreateRule(
        decimal price,
        bool isDefault,
        int sortOrder,
        int unitsPerSale = 1)
        => new()
        {
            ProductId = 1,
            Price = price,
            Currency = Currency.EUR,
            UnitsPerSale = unitsPerSale,
            SortOrder = sortOrder,
            IsDefault = isDefault
        };

    private sealed class ProductRepositoryFake(Product product) : IProductRepository
    {
        public Task<List<Product>> GetActiveProductsAsync(CancellationToken ct)
            => Task.FromResult(new List<Product> { product });

        public Task<List<Product>> GetActiveProductsWithPriceRulesAsync(CancellationToken ct)
            => Task.FromResult(new List<Product> { product });

        public Task<Product?> GetActiveProductWithPriceRulesAsync(int productId, CancellationToken ct)
            => Task.FromResult<Product?>(product.ProductId == productId ? product : null);
    }

    private sealed class PriceRuleRepositoryFake : IPriceRuleRepository
    {
        public int? SavedProductId { get; private set; }
        public List<PriceRule> SavedRules { get; private set; } = [];
        public PriceRule? RuleToReturn { get; init; }

        public Task<PriceRule?> GetRuleForProductAsync(int productId, decimal price, CancellationToken ct)
            => Task.FromResult(RuleToReturn);

        public Task<List<PriceRule>> GetAllPriceRulesAsync(CancellationToken ct)
            => Task.FromResult(new List<PriceRule>());

        public Task ReplacePriceRulesAsync(
            int productId,
            IReadOnlyCollection<PriceRule> priceRules,
            CancellationToken ct)
        {
            SavedProductId = productId;
            SavedRules = priceRules.ToList();
            return Task.CompletedTask;
        }
    }
}
