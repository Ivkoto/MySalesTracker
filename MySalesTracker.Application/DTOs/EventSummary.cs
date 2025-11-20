namespace MySalesTracker.Application.DTOs;

/// <summary>
/// Summary statistics for an entire event aggregated across all event days.
/// </summary>
public sealed class EventSummary
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public required string EventName { get; init; }

    /// <summary>
    /// The start date of the event.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// The end date of the event.
    /// </summary>
    public required DateOnly EndDate { get; init; }

    // Product counts (only for TOTEM and Candles)
    
    /// <summary>
    /// Total quantity of TOTEM products sold across all event days.
    /// </summary>
    public int TotemCount { get; init; }

    /// <summary>
    /// Total quantity of Candles (ГОРА) products sold across all event days.
    /// </summary>
    public int CandlesCount { get; init; }

    // Revenue by brand (net revenue after discounts)
    
    /// <summary>
    /// Total TOTEM revenue (price - discount) across all event days.
    /// </summary>
    public decimal TotemRevenue { get; init; }

    /// <summary>
    /// Total Ceramics revenue (price - discount) across all event days.
    /// </summary>
    public decimal CeramicsRevenue { get; init; }

    /// <summary>
    /// Total Candles revenue (price - discount) across all event days.
    /// </summary>
    public decimal CandlesRevenue { get; init; }

    /// <summary>
    /// Grand total revenue across all brands (price - discount) for all event days.
    /// </summary>
    public decimal TotalRevenue { get; init; }

    // Payments by method
    
    /// <summary>
    /// Total cash payments across all event days.
    /// </summary>
    public decimal CashTotal { get; init; }

    /// <summary>
    /// Total card payments across all event days.
    /// </summary>
    public decimal CardTotal { get; init; }

    /// <summary>
    /// Total Revolut Lidia payments across all event days.
    /// </summary>
    public decimal RevolutLidiaTotal { get; init; }

    /// <summary>
    /// Total Revolut Ivaylo payments across all event days.
    /// </summary>
    public decimal RevolutIvayloTotal { get; init; }
}

