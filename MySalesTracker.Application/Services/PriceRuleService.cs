using Microsoft.Extensions.Logging;
using MySalesTracker.Application.DTOs;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Application.Services;

public sealed class PriceRuleService(
    IPriceRuleRepository priceRuleRepository,
    IProductRepository productRepository,
    ILogger<PriceRuleService> logger)
{
    /// <summary>
    /// Gets the number of units for a configured product price.
    /// </summary>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="price">The price to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The configured units, or null when the price is no longer available.</returns>
    public async Task<int?> GetUnitsForProductAsync(int productId, decimal price, CancellationToken ct = default)
    {
        try
        {
            var priceRule = await priceRuleRepository.GetRuleForProductAsync(productId, price, ct);

            if (priceRule is null)
            {
                logger.LogWarning("No current price rule found for Product {ProductId}, Price {Price}.", productId, price);
                return null;
            }

            return priceRule.UnitsPerSale == 0 ? 1 : priceRule.UnitsPerSale;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get units for Product {ProductId}, Price {Price}", productId, price);
            throw;
        }
    }


    /// <summary>
    /// Retrieves all price rules from the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of all <see cref="PriceRule"/> entities. Returns an empty list if an error occurs.
    /// </returns>
    public async Task<List<PriceRule>> GetAllPriceRulesAsync(CancellationToken ct = default)
    {
        try
        {
            return await priceRuleRepository.GetAllPriceRulesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all price rules.");
            return [];
        }
    }

    public async Task<List<ProductPricing>> GetProductPricingAsync(CancellationToken ct = default)
    {
        var products = await productRepository.GetActiveProductsWithPriceRulesAsync(ct);
        return [.. products.Select(MapProductPricing)];
    }

    public async Task<ServiceResult<bool>> UpdateProductPriceRulesAsync(
        UpdateProductPriceRulesRequest request,
        CancellationToken ct = default)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return ServiceResult<bool>.FailureResult(validationError);
        }

        var product = await productRepository.GetActiveProductWithPriceRulesAsync(request.ProductId, ct);
        if (product is null)
        {
            return ServiceResult<bool>.FailureResult("Продуктът не е намерен.");
        }

        if (product.Brand == Brand.Ceramics)
        {
            return ServiceResult<bool>.FailureResult("Керамиката използва свободна цена и няма ценови правила.");
        }

        var priceRules = request.Rules
            .Select((rule, index) => new PriceRule
            {
                ProductId = request.ProductId,
                Price = rule.Price,
                Currency = Currency.EUR,
                UnitsPerSale = rule.UnitsPerSale,
                SortOrder = index + 1,
                IsDefault = rule.IsDefault
            })
            .ToList();

        try
        {
            await priceRuleRepository.ReplacePriceRulesAsync(
                request.ProductId,
                priceRules,
                ct);

            return ServiceResult<bool>.SuccessResult(true, "Цените са обновени.");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Price rules could not be updated for Product {ProductId}", request.ProductId);
            return ServiceResult<bool>.FailureResult(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update price rules for Product {ProductId}", request.ProductId);
            return ServiceResult<bool>.FailureResult("Цените не можаха да бъдат обновени.");
        }
    }

    private static string? ValidateRequest(UpdateProductPriceRulesRequest request)
    {
        if (request.Rules.Count == 0)
        {
            return "Добавете поне една цена.";
        }

        if (request.Rules.Count(r => r.IsDefault) != 1)
        {
            return "Изберете точно една основна цена.";
        }

        if (request.Rules.Any(r => r.Price <= 0 || r.Price > 9999.99m))
        {
            return "Цените трябва да бъдат между 0,01 и 9999,99 EUR.";
        }

        if (request.Rules.Any(r => decimal.Round(r.Price, 2) != r.Price))
        {
            return "Цените могат да имат най-много два знака след десетичната запетая.";
        }

        if (request.Rules.Any(r => r.UnitsPerSale <= 0))
        {
            return "Броят продукти трябва да бъде по-голям от нула.";
        }

        if (request.Rules.GroupBy(r => r.Price).Any(group => group.Count() > 1))
        {
            return "Една и съща цена не може да присъства повече от веднъж.";
        }

        return null;
    }

    private static ProductPricing MapProductPricing(Product product)
    {
        var currentRules = product.PriceRules
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.Price)
            .Select(MapPriceRule)
            .ToList();

        return new ProductPricing(
            product.ProductId,
            product.Name,
            product.Brand,
            product.Brand == Brand.Ceramics,
            currentRules);
    }

    private static PriceRuleDetails MapPriceRule(PriceRule rule)
        => new(
            rule.Price,
            rule.Currency,
            rule.UnitsPerSale,
            rule.SortOrder,
            rule.IsDefault);
}
