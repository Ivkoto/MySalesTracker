using Microsoft.EntityFrameworkCore;
using MySalesTracker.Data;
using MySalesTracker.DTOs;

namespace MySalesTracker.Services;

public class PriceRuleService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<PriceRuleService> _logger;

    public PriceRuleService(IDbContextFactory<AppDbContext> contextFactory, ILogger<PriceRuleService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the number of units per sale for a given product, price, and date.
    /// Returns 1 if no matching rule is found.
    /// </summary>
    public async Task<UnitsPerSale> GetUnitsForProductAsync(int productId, decimal price, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var priceRule = await context.PriceRules
                .Where(r => r.ProductId == productId
                    && r.Price == price
                    && r.EffectiveFrom <= onDate
                    && (r.EffectiveTo == null || r.EffectiveTo >= onDate))
                .OrderByDescending(r => r.EffectiveFrom)
                .ThenBy(r => r.SortOrder)
                .FirstOrDefaultAsync(cancellationToken);

            if (priceRule is null)
            {
                _logger.LogWarning($"No price rule found for Product {productId}, Price {price}, Date {onDate}. Defaulting to 1 unit per sale.");
                return new UnitsPerSale(1, null);
            }

            //TODO: remove when validation is implemented!
            // we don't expect to have 0 unit values in the database
            // but there’s no validation attribute, no fluent constraint, and no check in the seeder or any UI logic yet.
            // Until this kind of logic is not present everywhere, we will handle it here as a cheap extra safeguard.
            var units = priceRule.UnitsPerSale == 0 ? 1 : priceRule.UnitsPerSale;

            return new UnitsPerSale(units, priceRule.PriceRuleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get units for Product {ProductId}, Price {Price}, Date {Date}", productId, price, onDate);
            throw;
        }
    }
}
