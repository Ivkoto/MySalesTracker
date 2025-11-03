using Microsoft.EntityFrameworkCore;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Infrastructure.Persistence.Repositories;
internal class SaleRepository(IDbContextFactory<AppDbContext> contextFactory) : ISaleRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory = contextFactory;
    public async Task<List<Sale>> GetSalesByEventDay(int eventDayId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Sale
                .Include(s => s.Product)
                .Where(s => s.EventDayId == eventDayId)
                .OrderByDescending(s => s.SaleId)
                .ToListAsync(ct);
    }

    public async Task<Sale> CreateSaleAsync(Sale sale, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        context.Sale.Add(sale);
        await context.SaveChangesAsync(ct);

        return await context.Sale
            .Include(s => s.Product)
            .FirstAsync(s => s.SaleId == sale.SaleId, ct);
    }
}
