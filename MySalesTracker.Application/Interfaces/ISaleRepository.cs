using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Application.Interfaces;
public interface ISaleRepository
{
    Task<List<Sale>> GetSalesByEventDay(int eventDayId, CancellationToken ct = default);
    Task<Sale> CreateSaleAsync(Sale sale, CancellationToken ct = default);
}
