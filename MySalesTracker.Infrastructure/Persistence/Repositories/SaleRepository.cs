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
                .AsNoTracking()
                .Include(s => s.Product)
                .Where(s => s.EventDayId == eventDayId)
                .OrderByDescending(s => s.SaleId)
                .ToListAsync(ct);
    }

    public async Task<Sale?> GetSaleByIdAsync(int saleId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Sale
            .AsNoTracking()
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.SaleId == saleId, ct);
    }

    public async Task<Sale> CreateSaleAsync(Sale sale, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        context.Sale.Add(sale);
        await context.SaveChangesAsync(ct);

        // TODO: Can I skip this 2nd call?
        return await context.Sale
            .AsNoTracking()
            .Include(s => s.Product)
            .FirstAsync(s => s.SaleId == sale.SaleId, ct);
    }

    public async Task<Sale> UpdateSaleAsync(
        int saleId,
        decimal price,
        int quantityUnits,
        decimal discountValue,
        string? notes,
        int? priceRuleId,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        await context.Sale
            .Where(s => s.SaleId == saleId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Price, price)
                .SetProperty(s => s.QuantityUnits, quantityUnits)
                .SetProperty(s => s.DiscountValue, discountValue)
                .SetProperty(s => s.Notes, notes)
                .SetProperty(s => s.PriceRuleId, priceRuleId),
                ct);

        return await context.Sale
            .AsNoTracking()
            .Include(s => s.Product)
            .FirstAsync(s => s.SaleId == saleId, ct);
    }

    public async Task<bool> DeleteSaleAsync(int saleId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var rowsAffected = await context.Sale
            .Where(s => s.SaleId == saleId)
            .ExecuteDeleteAsync(ct);

        return rowsAffected > 0;
    }
}
