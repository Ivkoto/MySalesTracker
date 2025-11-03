using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Models;

namespace MySalesTracker.Application.Interfaces;
public interface IPriceRuleRepository
{
    Task<PriceRule?> GetUnitsForProduct(int productId, decimal price, DateOnly onDate, CancellationToken ct);
}
