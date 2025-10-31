using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MySalesTracker.Data;
using MySalesTracker.Data.Models;
using MySalesTracker.Domain;
using MySalesTracker.DTOs;
using MySalesTracker.Hubs;

namespace MySalesTracker.Services;

public sealed class SaleService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<SaleService> logger,
    IHubContext<SalesHub> hubContext)
{
    /// <summary>
    /// Gets all sales for a specific event day, including product information.
    /// </summary>
    /// <param name="eventDayId">The ID of the event day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="Sale"/> objects with <see cref="Product"/> navigation loaded,
    /// ordered by SaleId descending (most recent first). Returns an empty list if no sales exist for the event day.
    /// </returns>
    public async Task<List<Sale>> GetSalesByEventDayAsync(int eventDayId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Sale
                .Include(s => s.Product)
                .Where(s => s.EventDayId == eventDayId)
                .OrderByDescending(s => s.SaleId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve sales for EventDay {EventDayId}", eventDayId);
            throw;
        }
    }

    /// <summary>
    /// Creates a new sale and returns it with the product information loaded.
    /// Broadcasts a SignalR notification to clients subscribed to the event day group.
    /// </summary>
    /// <param name="eventDayId">The ID of the event day.</param>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="priceRuleId">The ID of the price rule (optional).</param>
    /// <param name="unitPrice">The unit price of the product.</param>
    /// <param name="quantityUnits">The quantity of units sold.</param>
    /// <param name="discountValue">The discount value applied to the sale.</param>
    /// <param name="notes">Any additional notes for the sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The newly created <see cref="Sale"/> with <see cref="Product"/> navigation loaded.
    /// A SignalR "SaleCreated" message is sent to all clients in the event day group after successful creation.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when unitPrice is less than or equal to zero, or when quantityUnits is less than or equal to zero.
    /// </exception>
    public async Task<Sale> CreateSaleAsync(
        int eventDayId,
        int productId,
        int? priceRuleId,
        decimal unitPrice,
        int quantityUnits,
        decimal discountValue = 0m,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validation = SalesCalculations.ValidateSaleInput(unitPrice, quantityUnits);
            if (!validation.IsValid)
                throw new ArgumentException(validation.ErrorMessage);

            logger.LogInformation(
                "Creating sale for EventDay {EventDayId}, Product {ProductId}, Price {Price}, Qty {Qty}",
                eventDayId, productId, unitPrice, quantityUnits);

            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

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

            await hubContext.Clients
                .Group(SalesHub.GroupNameForDay(eventDayId))
                .SendAsync("SaleCreated", eventDayId, savedSale.SaleId, cancellationToken);

            logger.LogInformation("Successfully created sale {SaleId}", savedSale.SaleId);
            return savedSale;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create sale for EventDay {EventDayId}", eventDayId);
            throw;
        }
    }

    /// <summary>
    /// Gets brand sales summaries with pre-computed aggregates for an event day.
    /// </summary>
    /// <param name="eventDayId">The ID of the event day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="BrandSalesSummary"/> objects grouped by <see cref="Brand"/>, 
    /// each containing aggregated totals (price, discount, quantity, count) and individual sales.
    /// Summaries are ordered by Brand descending. Returns an empty list if no sales exist for the event day.
    /// </returns>
    public async Task<List<BrandSalesSummary>> GetBrandSalesSummariesAsync(
        int eventDayId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            var sales = await GetSalesByEventDayAsync(eventDayId, cancellationToken);
            var summaries = SalesCalculations.GroupSalesByBrand(sales);

            logger.LogInformation(
                "Retrieved {BrandCount} brand summaries with {TotalSales} total sales for EventDay {EventDayId}",
                summaries.Count, summaries.Sum(s => s.SalesCount), eventDayId);

            return summaries;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get brand sales summaries for EventDay {EventDayId}", eventDayId);
            throw;
        }
    }
}
