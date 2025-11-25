using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Models;

namespace MySalesTracker.Application.Interfaces;
public interface IPriceRuleRepository
{
    Task<PriceRule?> GetUnitsForProductAsync(int productId, decimal price, DateOnly onDate, CancellationToken ct);
    Task<List<PriceRule>> GetAllPriceRulesAsync(DateOnly onDate, CancellationToken ct);
}
