using MySalesTracker.Domain.Enums;
using MySalesTracker.Domain.Models;

namespace MySalesTracker.Application.DTOs;

public sealed class EventStatisticsBundle
{
    public required EventSummary EventSummary { get; init; }
    public required List<EventDayInfo> EventDays { get; init; }
    public required List<DayStatistics> DaySummaries { get; init; }
}

public sealed class EventDayInfo
{
    public required int EventDayId { get; init; }
    public required DateOnly Date { get; init; }
}

public sealed class DayStatistics
{
    public required int EventDayId { get; init; }
    public required DateOnly Date { get; init; }
    public decimal? StartingPettyCash { get; init; }
    public required Currency StartingPettyCashCurrency { get; init; }
    public required Currency Currency { get; init; }
    public required PaymentSummary PaymentSummary { get; init; }
}
