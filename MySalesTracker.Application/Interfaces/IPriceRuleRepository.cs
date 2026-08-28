using MySalesTracker.Domain.Entities;
namespace MySalesTracker.Application.Interfaces;
public interface IPriceRuleRepository
{
    Task<PriceRule?> GetRuleForProductAsync(int productId, decimal price, CancellationToken ct);
    Task<List<PriceRule>> GetAllPriceRulesAsync(CancellationToken ct);
    Task ReplacePriceRulesAsync(int productId, IReadOnlyCollection<PriceRule> priceRules, CancellationToken ct);
}
