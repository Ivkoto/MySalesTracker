using Microsoft.Extensions.Logging;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Models;
using MySalesTracker.Domain.Services;

namespace MySalesTracker.Application.Services;

public sealed class SaleService(ISaleRepository saleRepository, ILogger<SaleService> logger, INotificationService notificationService)
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
    public async Task<List<Sale>> GetSalesByEventDayAsync(int eventDayId, CancellationToken ct = default)
    {
        try
        {
            return await saleRepository.GetSalesByEventDay(eventDayId, ct);
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
        CancellationToken  ct = default)
    {
        try
        {
            var validation = SalesCalculations.ValidateSaleInput(unitPrice, quantityUnits);
            if (!validation.IsValid)
                throw new ArgumentException(validation.ErrorMessage);

            logger.LogInformation(
                "Creating sale for EventDay {EventDayId}, Product {ProductId}, Price {Price}, Qty {Qty}",
                eventDayId, productId, unitPrice, quantityUnits);

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

            var savedSale = await saleRepository.CreateSaleAsync(sale, ct);

            await notificationService.NotifySaleCreatedAsync(eventDayId, savedSale.SaleId, ct);

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
    /// <summary>
    /// Gets a single sale by its ID, including product information.
    /// </summary>
    /// <param name="saleId">The ID of the sale.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The <see cref="Sale"/> object with <see cref="Product"/> navigation loaded, or null if not found.
    /// </returns>
    public async Task<Sale?> GetSaleByIdAsync(int saleId, CancellationToken ct = default)
    {
        try
        {
            return await saleRepository.GetSaleByIdAsync(saleId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve sale {SaleId}", saleId);
            throw;
        }
    }

    public async Task<List<BrandSalesSummary>> GetBrandSalesSummariesAsync(int eventDayId, CancellationToken ct = default)
    {
        try
        {
            var sales = await saleRepository.GetSalesByEventDay(eventDayId, ct);
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

    /// <summary>
    /// Updates an existing sale with new values.
    /// Broadcasts a SignalR notification to clients subscribed to the event day group.
    /// </summary>
    /// <param name="saleId">The ID of the sale to update.</param>
    /// <param name="eventDayId">The ID of the event day (for notification purposes).</param>
    /// <param name="unitPrice">The new unit price of the product.</param>
    /// <param name="quantityUnits">The new quantity of units sold.</param>
    /// <param name="discountValue">The new discount value applied to the sale.</param>
    /// <param name="notes">The new notes for the sale.</param>
    /// <param name="priceRuleId">The new price rule ID (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The updated <see cref="Sale"/> with <see cref="Product"/> navigation loaded.
    /// A SignalR "SaleCreated" message is sent to all clients in the event day group after successful update.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when unitPrice is less than or equal to zero, or when quantityUnits is less than or equal to zero.
    /// </exception>
    public async Task<Sale> UpdateSaleAsync(
        int saleId,
        int eventDayId,
        decimal unitPrice,
        int quantityUnits,
        decimal discountValue,
        string? notes,
        int? priceRuleId,
        CancellationToken ct = default)
    {
        try
        {
            var validation = SalesCalculations.ValidateSaleInput(unitPrice, quantityUnits);
            if (!validation.IsValid)
                throw new ArgumentException(validation.ErrorMessage);

            logger.LogInformation(
                "Updating sale {SaleId} for EventDay {EventDayId}, Price {Price}, Qty {Qty}",
                saleId, eventDayId, unitPrice, quantityUnits);

            var updatedSale = await saleRepository.UpdateSaleAsync(
                saleId, unitPrice, quantityUnits, discountValue, notes, priceRuleId, ct);

            await notificationService.NotifySaleCreatedAsync(eventDayId, saleId, ct);

            logger.LogInformation("Successfully updated sale {SaleId}", saleId);
            return updatedSale;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update sale {SaleId}", saleId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a sale by its ID.
    /// Broadcasts a SignalR notification to clients subscribed to the event day group.
    /// </summary>
    /// <param name="saleId">The ID of the sale to delete.</param>
    /// <param name="eventDayId">The ID of the event day (for notification purposes).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// True if the sale was deleted successfully, false if the sale was not found.
    /// A SignalR "SaleCreated" message is sent to all clients in the event day group after successful deletion.
    /// </returns>
    public async Task<bool> DeleteSaleAsync(int saleId, int eventDayId, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Deleting sale {SaleId} from EventDay {EventDayId}", saleId, eventDayId);

            var deleted = await saleRepository.DeleteSaleAsync(saleId, ct);

            if (deleted)
            {
                await notificationService.NotifySaleCreatedAsync(eventDayId, saleId, ct);
                logger.LogInformation("Successfully deleted sale {SaleId}", saleId);
            }
            else
            {
                logger.LogWarning("Sale {SaleId} not found for deletion", saleId);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete sale {SaleId}", saleId);
            throw;
        }
    }
}
