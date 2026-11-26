using MySalesTracker.Domain.Entities;

namespace MySalesTracker.Application.Interfaces;

/// <summary>
/// Repository for retrieving and managing sales recorded for event days.
/// </summary>
public interface ISaleRepository
{
    /// <summary>
    /// Retrieves the sales and their products for an event day.
    /// </summary>
    Task<List<Sale>> GetSalesByEventDay(int eventDayId, CancellationToken ct = default);

    /// <summary>
    /// Finds a sale by ID and includes its product.
    /// </summary>
    Task<Sale?> GetSaleByIdAsync(int saleId, CancellationToken ct = default);

    /// <summary>
    /// Saves a new sale and returns it with its product.
    /// </summary>
    Task<Sale> CreateSaleAsync(Sale sale, CancellationToken ct = default);

    /// <summary>
    /// Updates the recorded price, quantity, discount, and notes for a sale.
    /// </summary>
    Task<Sale> UpdateSaleAsync(int saleId, decimal price, int quantityUnits, decimal discountValue, string? notes, CancellationToken ct = default);

    /// <summary>
    /// Deletes a sale by ID and returns whether a record was removed.
    /// </summary>
    Task<bool> DeleteSaleAsync(int saleId, CancellationToken ct = default);
}
