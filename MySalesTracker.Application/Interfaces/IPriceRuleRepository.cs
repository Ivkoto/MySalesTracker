using MySalesTracker.Domain.Entities;
namespace MySalesTracker.Application.Interfaces;

/// <summary>
/// Repository for retrieving and replacing product price rules.
/// </summary>
public interface IPriceRuleRepository
{
    /// <summary>
    /// Finds the price rule for a product at the specified price.
    /// </summary>
    Task<PriceRule?> GetRuleForProductAsync(int productId, decimal price, CancellationToken ct);

    /// <summary>
    /// Retrieves all configured product price rules.
    /// </summary>
    Task<List<PriceRule>> GetAllPriceRulesAsync(CancellationToken ct);

    /// <summary>
    /// Replaces the entire set of price rules for a product.
    /// </summary>
    Task ReplacePriceRulesAsync(int productId, IReadOnlyCollection<PriceRule> priceRules, CancellationToken ct);
}
