using Microsoft.EntityFrameworkCore;
using MySalesTracker.Data;
using MySalesTracker.DTOs;

namespace MySalesTracker.Services;

public sealed class PriceRuleService(IDbContextFactory<AppDbContext> contextFactory, ILogger<PriceRuleService> logger)
{
    /// <summary>
    /// Gets the number of units per sale for a given product, price, and date.
    /// Returns 1 if no matching rule is found.
    /// </summary>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="price">The price to look up.</param>
    /// <param name="onDate">The date to check for the price rule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="UnitsPerSale"/> containing the number of units and optional price rule ID.
    /// Returns (1, null) if no matching rule is found, ensuring safe fallback behavior.
    /// The rule is selected based on effective date range and sorted by EffectiveFrom descending, then SortOrder ascending.
    /// </returns>
    public async Task<UnitsPerSale> GetUnitsForProductAsync(int productId, decimal price, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

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
                logger.LogWarning("No price rule found for Product {ProductId}, Price {Price}, Date {OnDate}. Defaulting to 1 unit per sale.", productId, price, onDate);
                return new UnitsPerSale(1, null);
            }

            // Safeguard against zero units until validation is implemented
            var units = priceRule.UnitsPerSale == 0 ? 1 : priceRule.UnitsPerSale;
            return new UnitsPerSale(units, priceRule.PriceRuleId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get units for Product {ProductId}, Price {Price}, Date {Date}", productId, price, onDate);
            throw;
        }
    }
}
