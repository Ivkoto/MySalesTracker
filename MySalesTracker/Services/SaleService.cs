using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MySalesTracker.Data;
using MySalesTracker.Data.Models;
using MySalesTracker.Models;
using MySalesTracker.Hubs;

namespace MySalesTracker.Services;

public class SaleService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<SaleService> _logger;
    private readonly IHubContext<SalesHub> _hubContext;

    public SaleService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<SaleService> logger,
        IHubContext<SalesHub> hubContext)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Gets all sales for a specific event day, including product information.
    /// </summary>
    public async Task<List<Sale>> GetSalesByEventDayAsync(int eventDayId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Sale
                .Include(s => s.Product)
                .Where(s => s.EventDayId == eventDayId)
                .OrderByDescending(s => s.SaleId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve sales for EventDay {EventDayId}", eventDayId);
            throw;
        }
    }

    /// <summary>
    /// Creates a new sale and returns it with the product information loaded.
    /// </summary>
    public async Task<Sale> CreateSaleAsync(
        int eventDayId, int productId, int? priceRuleId,
        decimal unitPrice, int quantityUnits,
        decimal discountValue = 0m, string? notes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (unitPrice <= 0)
                throw new ArgumentException("Unit price must be greater than zero", nameof(unitPrice));

            if (quantityUnits <= 0)
                throw new ArgumentException("Quantity units must be greater than zero", nameof(quantityUnits));

            _logger.LogInformation(
                "Creating sale for EventDay {EventDayId}, Product {ProductId}, Price {Price}, Qty {Qty}",
                eventDayId, productId, unitPrice, quantityUnits);

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var sale = new Sale
            {
                EventDayId = eventDayId,
                ProductId = productId,
                PriceRuleId = priceRuleId,
                Price = unitPrice,
                QuantityUnits = quantityUnits,
                DiscountValue = discountValue,
                Notes = notes,
                CreatedUtc = DateTime.UtcNow,
            };

            context.Sale.Add(sale);
            await context.SaveChangesAsync(cancellationToken);

            // Reload with product information
            var savedSale = await context.Sale
                .Include(s => s.Product)
                .FirstAsync(s => s.SaleId == sale.SaleId, cancellationToken);

            await _hubContext.Clients
                .Group(SalesHub.GroupNameForDay(eventDayId))
                .SendAsync("SaleCreated", eventDayId, savedSale.SaleId, cancellationToken);

            _logger.LogInformation("Successfully created sale {SaleId}", savedSale.SaleId);
            return savedSale;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sale for EventDay {EventDayId}", eventDayId);
            throw;
        }
    }

    /// <summary>
    /// Gets brand sales summaries with pre-computed aggregates like totals, counds, etc... for an event day.
    /// </summary>
    public async Task<List<BrandSalesSummary>> GetBrandSalesSummariesAsync(int eventDayId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var sales = await GetSalesByEventDayAsync(eventDayId, cancellationToken);

            var summaries = sales
                .GroupBy(s => s.Product.Brand)
                .Select(g => new BrandSalesSummary
                {
                    Brand = g.Key,
                    TotalPrice = g.Sum(s => s.Price),
                    TotalDiscount = g.Sum(s => s.DiscountValue),
                    TotalQuantityUnits = g.Sum(s => s.QuantityUnits),
                    SalesCount = g.Count(),
                    Sales = g.OrderByDescending(s => s.SaleId).ToList()
                })
                .OrderByDescending(bss => bss.Brand)
                .ToList();

            _logger.LogInformation(
                "Retrieved {BrandCount} brand summaries with {TotalSales} total sales for EventDay {EventDayId}",
                summaries.Count,
                summaries.Sum(s => s.SalesCount),
                eventDayId);

            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get brand sales summaries for EventDay {EventDayId}", eventDayId);
            throw;
        }
    }
}
