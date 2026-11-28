using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;
namespace MySalesTracker.Application.Interfaces;

/// <summary>
/// Repository for retrieving and replacing product price rules.
/// </summary>
public interface IPriceRuleRepository
{
    /// <summary>
    /// Finds the price rule for a product at the specified price in one price list.
    /// </summary>
    Task<PriceRule?> GetRuleForProductAsync(int productId, PriceList priceList, decimal price, CancellationToken ct);

    /// <summary>
    /// Retrieves the configured product price rules of one price list.
    /// </summary>
    Task<List<PriceRule>> GetAllPriceRulesAsync(PriceList priceList, CancellationToken ct);

    /// <summary>
    /// Replaces the entire set of price rules for a product within one price list.
    /// Rules of other price lists are left untouched.
    /// </summary>
    Task ReplacePriceRulesAsync(int productId, PriceList priceList, IReadOnlyCollection<PriceRule> priceRules, CancellationToken ct);
}
