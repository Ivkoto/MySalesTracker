using System.Globalization;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Application.Services;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;
using StatisticsPage = MySalesTracker.Web.Components.Pages.EventStatistics;

namespace MySalesTracker.Tests;

public sealed class EventStatisticsTests
{
    static readonly CultureInfo Bg = CultureInfo.GetCultureInfo("bg-BG");

    [Fact]
    public async Task PositiveEventDifferenceWithNegativeDaySum_ShowsDangerOnSumCellOnly()
    {
        var (renderer, id) = await OpenAsync(CreateEvent(day1Payments: 95m, day2Payments: 120m), "?days=1&view=comparison");

        var row = renderer.Row(id, "Разлика");

        Assert.Contains("table-success", row.RowClass);
        Assert.Equal(Money(15m), row.Cells[1].Text);
        Assert.Contains("table-danger", row.Cells[2].Class);
        Assert.Equal(Money(-5m), row.Cells[2].Text);
        Assert.Contains(row.Cells[3].DescendantClasses, value => value.Contains("bg-danger"));

        await renderer.CloseAsync(id);
    }

    [Fact]
    public async Task NegativeEventDifferenceWithPositiveDaySum_ShowsSuccessOnSumCellOnly()
    {
        var (renderer, id) = await OpenAsync(CreateEvent(day1Payments: 120m, day2Payments: 65m), "?days=1&view=comparison");

        var row = renderer.Row(id, "Разлика");

        Assert.Contains("table-danger", row.RowClass);
        Assert.Equal(Money(-15m), row.Cells[1].Text);
        Assert.Contains("table-success", row.Cells[2].Class);
        Assert.Equal(Money(20m), row.Cells[2].Text);
        Assert.Contains(row.Cells[3].DescendantClasses, value => value.Contains("bg-success"));

        await renderer.CloseAsync(id);
    }

    [Fact]
    public async Task ZeroDaySum_ShowsSuccessOnSumCell()
    {
        var (renderer, id) = await OpenAsync(CreateEvent(day1Payments: 105m, day2Payments: 95m), "?days=1,2&view=comparison");

        var row = renderer.Row(id, "Разлика");

        Assert.Equal(Money(0m), row.Cells[1].Text);
        Assert.Contains("table-success", row.Cells[2].Class);
        Assert.Equal(Money(0m), row.Cells[2].Text);
        Assert.Contains(row.Cells[3].DescendantClasses, value => value.Contains("bg-success"));
        Assert.Contains(row.Cells[4].DescendantClasses, value => value.Contains("bg-danger"));

        await renderer.CloseAsync(id);
    }

    static async Task<(StatisticsRenderer Renderer, int Id)> OpenAsync(Event evt, string query)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(new EventService(new FakeEventRepository(evt), NullLogger<EventService>.Instance))
            .AddSingleton<NavigationManager>(new TestNavigationManager($"https://localhost/statistics/compare-days/1{query}"))
            .BuildServiceProvider();

        var renderer = new StatisticsRenderer(services);
        var id = await renderer.OpenAsync(1);
        return (renderer, id);
    }

    static string Money(decimal value) => $"{value.ToString("0.00", Bg)} €";

    static Event CreateEvent(decimal day1Payments, decimal day2Payments)
    {
        var product = new Product { ProductId = 1, Name = "Свещ", Brand = Brand.Totem };

        return new Event
        {
            EventId = 1,
            Name = "Базар",
            StartDate = new DateOnly(2026, 11, 5),
            EndDate = new DateOnly(2026, 11, 6),
            Days =
            [
                CreateDay(1, new DateOnly(2026, 11, 5), product, day1Payments),
                CreateDay(2, new DateOnly(2026, 11, 6), product, day2Payments)
            ]
        };
    }

    static MySalesTracker.Domain.Entities.EventDay CreateDay(int id, DateOnly date, Product product, decimal paymentAmount)
        => new()
        {
            EventDayId = id,
            EventId = 1,
            Date = date,
            Sales =
            [
                new Sale
                {
                    SaleId = id,
                    EventDayId = id,
                    ProductId = product.ProductId,
                    Product = product,
                    Price = 100m,
                    QuantityUnits = 1,
                    CreatedUtc = new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc)
                }
            ],
            Payments =
            [
                new Payment
                {
                    PaymentId = id,
                    EventDayId = id,
                    Method = PaymentMethod.Cash,
                    Amount = paymentAmount
                }
            ]
        };

    sealed class FakeEventRepository(Event evt) : IEventRepository
    {
        public Task<List<Event>> GetAllEventsAsync(CancellationToken ct)
            => Task.FromResult(new List<Event> { evt });

        public Task<Event?> GetEventWithDaysOnlyAsync(int eventId, CancellationToken ct)
            => Task.FromResult<Event?>(evt.EventId == eventId ? evt : null);

        public Task<Event?> GetEventWithAllDataAsync(int eventId, CancellationToken ct)
            => Task.FromResult<Event?>(evt.EventId == eventId ? evt : null);

        public Task<List<MySalesTracker.Domain.Entities.EventDay>> GetEventDaysWithDataAsync(
            int eventId,
            IReadOnlyCollection<int> dayIds,
            CancellationToken ct)
            => Task.FromResult(new List<MySalesTracker.Domain.Entities.EventDay>());

        public Task<List<(string Name, DateOnly StartDate, DateOnly EndDate)>> GetExistingEventsByYearAsync(int year, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Event> CreateEventAsync(Event createdEvent, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<List<Event>> GetEventsWithAllDataAsync(IReadOnlyCollection<int> eventIds, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<MySalesTracker.Domain.Entities.EventDay?> GetEventDayByIdAsync(int id, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<MySalesTracker.Domain.Entities.EventDay?> UpdateStartingPettyCashAsync(int eventDayId, decimal? amount, CancellationToken ct)
            => throw new NotSupportedException();
    }

    sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string uri) => Initialize("https://localhost/", uri);

        protected override void NavigateToCore(string uri, bool forceLoad) => Uri = Absolute(uri);

        protected override void NavigateToCore(string uri, NavigationOptions options) => Uri = Absolute(uri);

        string Absolute(string uri) => new Uri(new Uri(BaseUri), uri).ToString();
    }

    sealed record CellSnapshot(string Class, string Text, IReadOnlyList<string> DescendantClasses);

    sealed record RowSnapshot(string RowClass, IReadOnlyList<CellSnapshot> Cells);

#pragma warning disable BL0006
    sealed class StatisticsRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;

        protected override void HandleException(Exception exception)
            => ExceptionDispatchInfo.Capture(exception).Throw();

        public Task<int> OpenAsync(int eventId)
            => Dispatcher.InvokeAsync(async () =>
            {
                var id = AssignRootComponentId(InstantiateComponent(typeof(StatisticsPage)));
                await RenderRootComponentAsync(id, ParameterView.FromDictionary(
                    new Dictionary<string, object?> { ["EventId"] = eventId }));
                return id;
            });

        public Task CloseAsync(int id)
            => Dispatcher.InvokeAsync(() => RemoveRootComponent(id));

        public RowSnapshot Row(int id, string label)
        {
            var frames = Frames(id).ToArray();
            for (var index = 0; index < frames.Length; index++)
            {
                if (frames[index].FrameType != RenderTreeFrameType.Element || frames[index].ElementName != "tr")
                {
                    continue;
                }

                var attributes = frames.Skip(index + 1)
                    .TakeWhile(frame => frame.FrameType == RenderTreeFrameType.Attribute)
                    .ToArray();
                var subtree = frames.Skip(index + 1).Take(frames[index].ElementSubtreeLength - 1).ToArray();
                var cells = Cells(subtree);
                if (cells.Count == 0 || cells[0].Text != label)
                {
                    continue;
                }

                return new RowSnapshot(Attribute(attributes, "class") ?? string.Empty, cells);
            }

            throw new InvalidOperationException($"Row not found: {label}");
        }

        static List<CellSnapshot> Cells(RenderTreeFrame[] rowFrames)
        {
            var cells = new List<CellSnapshot>();
            for (var index = 0; index < rowFrames.Length; index++)
            {
                if (rowFrames[index].FrameType != RenderTreeFrameType.Element || rowFrames[index].ElementName != "td")
                {
                    continue;
                }

                var attributes = rowFrames.Skip(index + 1)
                    .TakeWhile(frame => frame.FrameType == RenderTreeFrameType.Attribute)
                    .ToArray();
                var subtree = rowFrames.Skip(index + 1)
                    .Take(rowFrames[index].ElementSubtreeLength - 1)
                    .ToArray();
                cells.Add(new CellSnapshot(
                    Attribute(attributes, "class") ?? string.Empty,
                    string.Join(" ", subtree
                        .Where(frame => frame.FrameType == RenderTreeFrameType.Text)
                        .Select(frame => frame.TextContent)).Trim(),
                    subtree
                        .Where(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeName == "class")
                        .Select(frame => frame.AttributeValue?.ToString())
                        .OfType<string>()
                        .ToList()));
            }

            return cells;
        }

        static string? Attribute(RenderTreeFrame[] frames, string name)
            => frames.FirstOrDefault(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeName == name)
                .AttributeValue?.ToString();

        IEnumerable<RenderTreeFrame> Frames(int id)
        {
            var frames = GetCurrentRenderTreeFrames(id);
            return frames.Array.Take(frames.Count);
        }
    }
#pragma warning restore BL0006
}
