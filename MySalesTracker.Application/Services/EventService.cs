using Microsoft.Extensions.Logging;
using MySalesTracker.Application.DTOs;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;
using MySalesTracker.Domain.Services;

namespace MySalesTracker.Application.Services;

public sealed class EventService(IEventRepository eventRepository, ILogger<EventService> logger)
{
    /// <summary>
    /// Gets all events that overlap with the specified year.
    /// </summary>
    /// <param name="year">The year to filter events by. Only the year component is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of tuples containing the event name, start date, and end date for all events
    /// where either the start date or end date falls within the specified year.
    /// Returns an empty list if no events match the criteria.
    /// </returns>
    public async Task<List<(string, DateOnly, DateOnly)>> GetExistingEventsByYearAsync(DateOnly year, CancellationToken cancellationToken = default)
    {
        //TODO: ⬆️ Consider Task<SomeModel> instead of list of tuples for better clarity.
        try
        {
            return await eventRepository.GetExistingEventsByYearAsync(year.Year, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve events");
            throw;
        }

    }

    /// <summary>
    /// Gets all events with their days, ordered by EventId descending.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="Event"/> objects with their associated days loaded,
    /// ordered by most recent event first. Returns an empty list if no events exist.
    /// </returns>
    public async Task<List<Event>> GetAllEventsAsync(CancellationToken ct = default)
    {
        try
        {
            return await eventRepository.GetAllEventsAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve events");
            throw;
        }
    }

    /// <summary>
    /// Creates a new event with the specified date range and returns it with generated event days.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <param name="startDate">The start date of the event.</param>
    /// <param name="endDate">The end date of the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The newly created <see cref="Event"/> with auto-generated <see cref="EventDay"/> entries
    /// for each day in the specified range (inclusive).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when name is empty or whitespace, or when end date is before start date.
    /// </exception>
    public async Task<ServiceResult<Event>> CreateEventAsync(string name, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
    {
        try
        {
            var existingEvents = await GetExistingEventsByYearAsync(startDate, ct);

            var validation = EventValidations.ValidateCreateEvent(name, startDate, endDate, existingEvents);
            if (!validation.IsValid)
            {
                logger.LogWarning("Event creation validation failed: {ErrorMessage}", validation.ErrorMessage);
                return ServiceResult<Event>.FailureResult(validation.ErrorMessage);
            }                

            logger.LogInformation("Creating event '{Name}' from {StartDate} to {EndDate}", name, startDate, endDate);

            var evt = new Event { Name = name, StartDate = startDate, EndDate = endDate };
            var dates = EventValidations.GenerateDateRange(startDate, endDate);
            foreach (var date in dates)
            {
                evt.Days.Add(new EventDay { Date = date });
            }

            var savedEvent = await eventRepository.CreateEventAsync(evt, ct);

            logger.LogInformation("Successfully created event {EventId} with {DayCount} days", savedEvent.EventId, savedEvent.Days.Count);
            return ServiceResult<Event>.SuccessResult(savedEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create event '{Name}'", name);
            throw;
        }
    }

    /// <summary>
    /// Gets a specific event day by ID, including the parent event information.
    /// </summary>
    /// <param name="eventDayId">The ID of the event day to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="EventDay"/> with the parent <see cref="Event"/> loaded if found;
    /// otherwise null. A warning is logged when the event day is not found.
    /// </returns>
    public async Task<EventDay?> GetEventDayByIdAsync(int eventDayId, CancellationToken ct = default)
    {
        try
        {
            var evtDay = await eventRepository.GetEventDayByIdAsync(eventDayId, ct);

            if (evtDay is null)
            {
                logger.LogWarning("EventDay with ID {EventDayId} not found", eventDayId);
            }

            return evtDay;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database error while retrieving EventDay with ID {EventDayId}", eventDayId);
            throw;
        }
    }

    /// <summary>
    /// Gets aggregated summary statistics for an entire event across all its days.
    /// </summary>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ServiceResult{EventSummary}"/> representing the operation result. On success
    /// the result contains the aggregated <see cref="EventSummary"/>; on failure it contains
    /// an error message explaining the problem.
    /// </returns>
    public async Task<ServiceResult<EventSummary>> GetEventSummaryAsync(int eventId, CancellationToken ct = default)
    {
        try
        {
            var evt = await eventRepository.GetEventWithAllDataAsync(eventId, ct);

            if (evt is null)
            {
                logger.LogWarning("Event with ID {EventId} not found", eventId);
                return ServiceResult<EventSummary>.FailureResult("Събитието не е намерено");
            }

            // Aggregate all sales across all event days
            var allSales = evt.Days.SelectMany(d => d.Sales).ToList();
            var allPayments = evt.Days.SelectMany(d => d.PaymentsCounted).ToList();

            // Calculate counts by brand (only TOTEM and Candles)
            var totemCount = allSales
                .Where(s => s.Product.Brand == Brand.Totem)
                .Sum(s => s.QuantityUnits);

            var candlesCount = allSales
                .Where(s => s.Product.Brand == Brand.Candles)
                .Sum(s => s.QuantityUnits);

            // Calculate revenue by brand (price - discount)
            var totemRevenue = allSales
                .Where(s => s.Product.Brand == Brand.Totem)
                .Sum(s => s.Price - s.DiscountValue);

            var ceramicsRevenue = allSales
                .Where(s => s.Product.Brand == Brand.Ceramics)
                .Sum(s => s.Price - s.DiscountValue);

            var candlesRevenue = allSales
                .Where(s => s.Product.Brand == Brand.Candles)
                .Sum(s => s.Price - s.DiscountValue);

            var totalRevenue = totemRevenue + ceramicsRevenue + candlesRevenue;

            // Calculate payment totals by method
            var cashTotal = allPayments
                .Where(p => p.Method == PaymentMethod.Cash)
                .Sum(p => p.Amount);

            var cardTotal = allPayments
                .Where(p => p.Method == PaymentMethod.Card)
                .Sum(p => p.Amount);

            var revolutLidiaTotal = allPayments
                .Where(p => p.Method == PaymentMethod.RevolutLidia)
                .Sum(p => p.Amount);

            var revolutIvayloTotal = allPayments
                .Where(p => p.Method == PaymentMethod.RevolutIvaylo)
                .Sum(p => p.Amount);

            logger.LogInformation(
                "Generated summary for Event {EventId}: TotalRevenue={TotalRevenue}, TotalPayments={TotalPayments}",
                eventId, totalRevenue, cashTotal + cardTotal + revolutLidiaTotal + revolutIvayloTotal);

            var summary = new EventSummary
            {
                EventName = evt.Name,
                StartDate = evt.StartDate,
                EndDate = evt.EndDate,
                TotemCount = totemCount,
                CandlesCount = candlesCount,
                TotemRevenue = totemRevenue,
                CeramicsRevenue = ceramicsRevenue,
                CandlesRevenue = candlesRevenue,
                TotalRevenue = totalRevenue,
                CashTotal = cashTotal,
                CardTotal = cardTotal,
                RevolutLidiaTotal = revolutLidiaTotal,
                RevolutIvayloTotal = revolutIvayloTotal
            };

            return ServiceResult<EventSummary>.SuccessResult(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get event summary for Event {EventId}", eventId);
            return ServiceResult<EventSummary>.FailureResult($"Грешка при зареждане на статистика: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the starting petty cash amount for a specific <see cref="EventDay"/>.
    /// </summary>
    /// <param name="eventDayId">Identifier of the event day to update.</param>
    /// <param name="amount">New starting petty cash amount; if null, the value is cleared.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ServiceResult{EventDay}"/> containing the updated <see cref="EventDay"/>
    /// when successful, otherwise a failure result with an error message.
    /// </returns>
    public async Task<ServiceResult<EventDay>> UpdateStartingPettyCashAsync(int eventDayId, decimal? amount, CancellationToken ct = default)
    {
        try
        {
            var evtDay = await eventRepository.UpdateStartingPettyCashAsync(eventDayId, amount, ct);

            if (evtDay is null)
            {
                logger.LogWarning("EventDay with {ID} cannot be found", eventDayId);
                return ServiceResult<EventDay>.FailureResult("Денят не е намерен");
            }

            logger.LogInformation("Updated StartingPettyCash for EventDay {EventDayId}", eventDayId);
            return ServiceResult<EventDay>.SuccessResult(evtDay, "Взети Оборотни са записани!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while updating StartingPettyCash for EventDay {EventDayId}", eventDayId);
            return ServiceResult<EventDay>.FailureResult($"Грешка при обновяване: {ex.Message}");
        }
    }
}
