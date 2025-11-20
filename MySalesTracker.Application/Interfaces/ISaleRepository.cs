using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Application.Interfaces;
public interface ISaleRepository
{
    Task<List<Sale>> GetSalesByEventDay(int eventDayId, CancellationToken ct = default);
    Task<Sale?> GetSaleByIdAsync(int saleId, CancellationToken ct = default);
    Task<Sale> CreateSaleAsync(Sale sale, CancellationToken ct = default);
    Task<Sale> UpdateSaleAsync(int saleId, decimal price, int quantityUnits, decimal discountValue, string? notes, int? priceRuleId, CancellationToken ct = default);
    Task<bool> DeleteSaleAsync(int saleId, CancellationToken ct = default);
}
