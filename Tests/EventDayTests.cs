using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Application.Services;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;
using EventDayPage = MySalesTracker.Web.Components.Pages.EventDay;

namespace MySalesTracker.Tests;

public sealed class EventDayTests
{
    [Fact]
    public async Task ProductCreated_RefreshesOpenPickerBeforeFirstSaleAndKeepsSelection()
    {
        var existingProduct = new Product { ProductId = 1, Name = "Купа", Brand = Brand.Ceramics };
        var newProduct = new Product { ProductId = 2, Name = "Шапка", Brand = Brand.Totem };
        var productRepository = new ProductRepositoryFake(existingProduct);
        var services = CreateServices(
            new FakeEventRepository(CreateDay()),
            productRepository,
            new FakeSaleRepository(),
            new FakePriceRuleRepository());

        await using var renderer = new EventDayRenderer(services);
        var id = await renderer.OpenAsync(1);

        await renderer.SelectProductAsync(id, "1");
        Assert.DoesNotContain("2", renderer.OptionValues(id));

        await productRepository.TryAddAsync(newProduct, TestContext.Current.CancellationToken);
        await renderer.ReceiveProductCreatedAsync(id);

        Assert.Contains("2", renderer.OptionValues(id));
        Assert.True(renderer.HasInputWithPlaceholder(id, "Цена…"));
        await renderer.CloseAsync(id);
    }

    [Fact]
    public async Task RefreshingSales_NewProduct_AppearsInPickerAndSaleCanBeEdited()
    {
        var existingProduct = new Product { ProductId = 1, Name = "Свещ", Brand = Brand.Totem };
        var newProduct = new Product { ProductId = 2, Name = "Нов", Brand = Brand.Ceramics };
        var sale = CreateSale(10, newProduct, price: 12.5m, quantityUnits: 1);

        var productRepository = new PickerProductRepositoryFake(existingProduct)
        {
            OnGetActiveProducts = calls => calls == 1 ? [existingProduct] : [existingProduct, newProduct]
        };
        var saleRepository = new FakeSaleRepository(sale);
        var services = CreateServices(
            new FakeEventRepository(CreateDay()),
            productRepository,
            saleRepository,
            new FakePriceRuleRepository());

        await using var renderer = new EventDayRenderer(services);
        var id = await renderer.OpenAsync(1);

        Assert.Equal(2, productRepository.GetActiveProductsCalls);
        Assert.Contains("2", renderer.OptionValues(id));

        await renderer.ClickEditAsync(id);

        Assert.Equal(0, renderer.SelectCount(id));
        Assert.True(renderer.HasInputWithPlaceholder(id, "Цена…"));
        Assert.Contains("Нов", renderer.Text(id));

        await renderer.ClickButtonAsync(id, "Запази");

        var updated = saleRepository.UpdatedSale;
        Assert.NotNull(updated);
        Assert.Equal(12.5m, updated!.Price);
        Assert.Equal(1, updated.QuantityUnits);
        Assert.Equal(1, renderer.SelectCount(id));
        await renderer.CloseAsync(id);
    }

    [Fact]
    public async Task StalePicker_SaleCanStillBeEditedAndSaved()
    {
        var existingProduct = new Product { ProductId = 1, Name = "Свещ", Brand = Brand.Totem };
        var newProduct = new Product { ProductId = 2, Name = "Нов", Brand = Brand.Ceramics };
        var sale = CreateSale(10, newProduct, price: 12.5m, quantityUnits: 1);

        var productRepository = new PickerProductRepositoryFake(existingProduct)
        {
            OnGetActiveProducts = _ => [existingProduct]
        };
        var saleRepository = new FakeSaleRepository(sale);
        var services = CreateServices(
            new FakeEventRepository(CreateDay()),
            productRepository,
            saleRepository,
            new FakePriceRuleRepository());

        await using var renderer = new EventDayRenderer(services);
        var id = await renderer.OpenAsync(1);

        Assert.Equal(2, productRepository.GetActiveProductsCalls);
        Assert.DoesNotContain("2", renderer.OptionValues(id));

        await renderer.ClickEditAsync(id);

        Assert.Equal(2, productRepository.GetActiveProductsCalls);
        Assert.True(renderer.HasInputWithPlaceholder(id, "Цена…"));
        Assert.Contains("Нов", renderer.Text(id));

        await renderer.ClickButtonAsync(id, "Запази");

        var updated = saleRepository.UpdatedSale;
        Assert.NotNull(updated);
        Assert.Equal(12.5m, updated!.Price);
        await renderer.CloseAsync(id);
    }

    [Fact]
    public async Task InactiveHistoricalProduct_CanBeEditedAndIsClearedBeforeNextSale()
    {
        var existingProduct = new Product { ProductId = 1, Name = "Свещ", Brand = Brand.Totem };
        var inactiveProduct = new Product { ProductId = 3, Name = "Архив", Brand = Brand.Totem, IsActive = false };
        var sale = CreateSale(20, inactiveProduct, price: 10m, quantityUnits: 2);
        var priceRules = new FakePriceRuleRepository();
        priceRules.Rules.Add(new PriceRule
        {
            ProductId = inactiveProduct.ProductId,
            Price = 10m,
            Currency = Currency.EUR,
            UnitsPerSale = 2,
            SortOrder = 1,
            IsDefault = true
        });

        var productRepository = new PickerProductRepositoryFake(existingProduct);
        var saleRepository = new FakeSaleRepository(sale);
        var services = CreateServices(
            new FakeEventRepository(CreateDay()),
            productRepository,
            saleRepository,
            priceRules);

        await using var renderer = new EventDayRenderer(services);
        var id = await renderer.OpenAsync(1);

        Assert.Equal(1, productRepository.GetActiveProductsCalls);

        await renderer.ClickEditAsync(id);

        Assert.Contains("Архив", renderer.Text(id));
        Assert.Equal(1, renderer.SelectCount(id));
        Assert.Contains("10", renderer.OptionValues(id));

        await renderer.ClickButtonAsync(id, "Запази");

        var updated = saleRepository.UpdatedSale;
        Assert.NotNull(updated);
        Assert.Equal(10m, updated!.Price);
        Assert.Equal(2, updated.QuantityUnits);
        Assert.Equal(1, productRepository.GetActiveProductsCalls);
        Assert.DoesNotContain("10", renderer.OptionValues(id));

        var ct = TestContext.Current.CancellationToken;
        using var addTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        addTimeout.CancelAfter(TimeSpan.FromSeconds(5));
        while (!await renderer.Dispatcher.InvokeAsync(() => renderer.Text(id).Contains("Добави", StringComparison.Ordinal)))
        {
            await Task.Delay(25, addTimeout.Token);
        }

        await renderer.ClickButtonAsync(id, "Добави");

        Assert.Contains("Посочи какво продаваш", renderer.Text(id));
        Assert.Single(await saleRepository.GetSalesByEventDay(sale.EventDayId, ct));
        await renderer.CloseAsync(id);
    }

    static ServiceProvider CreateServices(
        IEventRepository eventRepository,
        IProductRepository productRepository,
        ISaleRepository saleRepository,
        IPriceRuleRepository priceRuleRepository)
        => new ServiceCollection()
            .AddLogging()
            .AddSingleton(new EventService(eventRepository, NullLogger<EventService>.Instance))
            .AddSingleton(new ProductService(productRepository, NullLogger<ProductService>.Instance))
            .AddSingleton(new PriceRuleService(priceRuleRepository, productRepository, NullLogger<PriceRuleService>.Instance))
            .AddSingleton(new SaleService(saleRepository, NullLogger<SaleService>.Instance, new NoOpNotificationService()))
            .AddSingleton<NavigationManager>(new TestNavigationManager())
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton<IJSRuntime>(new EventDayJsFake())
            .BuildServiceProvider();

    static MySalesTracker.Domain.Entities.EventDay CreateDay()
        => new()
        {
            EventDayId = 1,
            EventId = 1,
            Date = new DateOnly(2026, 11, 5),
            Event = new Event
            {
                EventId = 1,
                Name = "Базар",
                StartDate = new DateOnly(2026, 11, 5),
                EndDate = new DateOnly(2026, 11, 8)
            }
        };

    static Sale CreateSale(int saleId, Product product, decimal price, int quantityUnits)
        => new()
        {
            SaleId = saleId,
            EventDayId = 1,
            ProductId = product.ProductId,
            Product = product,
            Price = price,
            QuantityUnits = quantityUnits,
            CreatedUtc = new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc)
        };

    sealed class TestEventDay : EventDayPage
    {
        protected override Task EnsureHubConnectionAsync() => Task.CompletedTask;

        public Task ReceiveProductCreatedAsync() => RefreshProductPickerAsync();
    }

    sealed class FakeEventRepository(MySalesTracker.Domain.Entities.EventDay day) : IEventRepository
    {
        public Task<MySalesTracker.Domain.Entities.EventDay?> GetEventDayByIdAsync(int id, CancellationToken ct)
            => Task.FromResult<MySalesTracker.Domain.Entities.EventDay?>(day);

        public Task<List<(string Name, DateOnly StartDate, DateOnly EndDate)>> GetExistingEventsByYearAsync(int year, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<List<Event>> GetAllEventsAsync(CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Event> CreateEventAsync(Event evt, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Event?> GetEventWithAllDataAsync(int eventId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<List<Event>> GetEventsWithAllDataAsync(IReadOnlyCollection<int> eventIds, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Event?> GetEventWithDaysOnlyAsync(int eventId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<List<MySalesTracker.Domain.Entities.EventDay>> GetEventDaysWithDataAsync(int eventId, IReadOnlyCollection<int> dayIds, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<MySalesTracker.Domain.Entities.EventDay?> UpdateStartingPettyCashAsync(int eventDayId, decimal? amount, CancellationToken ct)
            => throw new NotSupportedException();
    }

    sealed class PickerProductRepositoryFake(params Product[] products) : IProductRepository
    {
        private readonly List<Product> products = [.. products];

        public int GetActiveProductsCalls { get; private set; }

        public Func<int, List<Product>>? OnGetActiveProducts { get; init; }

        public Task<bool> TryAddAsync(Product product, CancellationToken ct)
            => Task.FromResult(false);

        public Task<List<Product>> GetActiveProductsAsync(CancellationToken ct)
        {
            GetActiveProductsCalls++;
            return Task.FromResult(OnGetActiveProducts?.Invoke(GetActiveProductsCalls) ?? [.. products]);
        }

        public Task<List<Product>> GetActiveProductsWithPriceRulesAsync(CancellationToken ct)
            => GetActiveProductsAsync(ct);

        public Task<Product?> GetActiveProductWithPriceRulesAsync(int productId, CancellationToken ct)
            => Task.FromResult(products.FirstOrDefault(product => product.ProductId == productId));
    }

    sealed class FakeSaleRepository(params Sale[] initialSales) : ISaleRepository
    {
        private readonly List<Sale> sales = [.. initialSales];

        public Sale? UpdatedSale { get; private set; }

        public Task<List<Sale>> GetSalesByEventDay(int eventDayId, CancellationToken ct = default)
            => Task.FromResult(sales.Where(sale => sale.EventDayId == eventDayId).ToList());

        public Task<Sale?> GetSaleByIdAsync(int saleId, CancellationToken ct = default)
            => Task.FromResult(sales.FirstOrDefault(sale => sale.SaleId == saleId));

        public Task<Sale> CreateSaleAsync(Sale sale, CancellationToken ct = default)
        {
            sales.Add(sale);
            return Task.FromResult(sale);
        }

        public Task<Sale> UpdateSaleAsync(
            int saleId,
            decimal price,
            int quantityUnits,
            decimal discountValue,
            string? notes,
            CancellationToken ct = default)
        {
            var existing = sales.Single(sale => sale.SaleId == saleId);
            var updated = new Sale
            {
                SaleId = existing.SaleId,
                EventDayId = existing.EventDayId,
                ProductId = existing.ProductId,
                Product = existing.Product,
                Price = price,
                QuantityUnits = quantityUnits,
                DiscountValue = discountValue,
                Notes = notes,
                Currency = existing.Currency,
                CreatedUtc = existing.CreatedUtc
            };

            sales[sales.IndexOf(existing)] = updated;
            UpdatedSale = updated;
            return Task.FromResult(updated);
        }

        public Task<bool> DeleteSaleAsync(int saleId, CancellationToken ct = default)
            => Task.FromResult(sales.RemoveAll(sale => sale.SaleId == saleId) > 0);
    }

    sealed class FakePriceRuleRepository : IPriceRuleRepository
    {
        public List<PriceRule> Rules { get; } = [];

        public Task<PriceRule?> GetRuleForProductAsync(int productId, decimal price, CancellationToken ct)
            => Task.FromResult(Rules.FirstOrDefault(rule => rule.ProductId == productId && rule.Price == price));

        public Task<List<PriceRule>> GetAllPriceRulesAsync(CancellationToken ct)
            => Task.FromResult(Rules.ToList());

        public Task ReplacePriceRulesAsync(int productId, IReadOnlyCollection<PriceRule> priceRules, CancellationToken ct)
        {
            Rules.RemoveAll(rule => rule.ProductId == productId);
            Rules.AddRange(priceRules);
            return Task.CompletedTask;
        }
    }

    sealed class NoOpNotificationService : INotificationService
    {
        public Task NotifySaleCreatedAsync(int eventDayId, int saleId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task NotifyEventCreatedAsync(int eventId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task NotifyProductCreatedAsync(CancellationToken ct = default)
            => Task.CompletedTask;
    }

    sealed class EventDayJsFake : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => ValueTask.FromResult(default(TValue)!);
    }

    sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");
    }

#pragma warning disable BL0006
    sealed class EventDayRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;

        protected override void HandleException(Exception exception)
            => ExceptionDispatchInfo.Capture(exception).Throw();

        public Task<int> OpenAsync(int dayId)
            => Dispatcher.InvokeAsync(async () =>
            {
                var id = AssignRootComponentId(InstantiateComponent(typeof(TestEventDay)));
                await RenderRootComponentAsync(id, ParameterView.FromDictionary(
                    new Dictionary<string, object?> { ["id"] = dayId }));
                return id;
            });

        public Task CloseAsync(int id)
            => Dispatcher.InvokeAsync(() => RemoveRootComponent(id));

        public Task ReceiveProductCreatedAsync(int id)
            => ((TestEventDay)GetComponentState(id).Component).ReceiveProductCreatedAsync();

        public Task SelectProductAsync(int id, string value)
            => Dispatcher.InvokeAsync(() =>
            {
                var handler = Elements(id, "select").First().Single(frame => frame.AttributeName == "onchange");
                return DispatchEventAsync(handler.AttributeEventHandlerId, null, new ChangeEventArgs { Value = value });
            });

        public string Text(int id)
            => string.Join(" ", Frames(id)
                .Where(frame => frame.FrameType == RenderTreeFrameType.Text)
                .Select(frame => frame.TextContent));

        public int SelectCount(int id) => Elements(id, "select").Count();

        public IReadOnlyList<string> OptionValues(int id)
            => Elements(id, "option")
                .Select(attributes => Attribute(attributes, "value"))
                .OfType<string>()
                .ToList();

        public bool HasInputWithPlaceholder(int id, string placeholder)
            => Elements(id, "input").Any(attributes => Attribute(attributes, "placeholder") == placeholder);

        public Task ClickEditAsync(int id)
            => Dispatcher.InvokeAsync(() =>
            {
                var attributes = Elements(id, "button")
                    .FirstOrDefault(candidate => Attribute(candidate, "title") == "Редактирай")
                    ?? throw new InvalidOperationException("Edit button not found.");
                return DispatchEventAsync(HandlerId(attributes), null, new MouseEventArgs());
            });

        public Task ClickButtonAsync(int id, string text)
            => Dispatcher.InvokeAsync(() =>
            {
                var frames = Frames(id).ToArray();
                for (var index = 0; index < frames.Length; index++)
                {
                    if (frames[index].FrameType != RenderTreeFrameType.Element || frames[index].ElementName != "button")
                    {
                        continue;
                    }

                    var subtree = frames.Skip(index + 1).Take(frames[index].ElementSubtreeLength - 1).ToArray();
                    var label = string.Join(" ", subtree
                        .Where(frame => frame.FrameType == RenderTreeFrameType.Text)
                        .Select(frame => frame.TextContent));

                    if (!label.Contains(text, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var handler = subtree.First(frame => frame.FrameType == RenderTreeFrameType.Attribute
                        && frame.AttributeName == "onclick");
                    return DispatchEventAsync(handler.AttributeEventHandlerId, null, new MouseEventArgs());
                }

                throw new InvalidOperationException($"Button not found: {text}");
            });

        static ulong HandlerId(RenderTreeFrame[] attributes)
            => attributes.First(frame => frame.AttributeName == "onclick").AttributeEventHandlerId;

        static string? Attribute(RenderTreeFrame[] attributes, string name)
            => attributes.FirstOrDefault(frame => frame.AttributeName == name).AttributeValue?.ToString();

        IEnumerable<RenderTreeFrame[]> Elements(int id, string elementName)
        {
            var frames = Frames(id).ToArray();
            for (var index = 0; index < frames.Length; index++)
            {
                if (frames[index].FrameType != RenderTreeFrameType.Element || frames[index].ElementName != elementName)
                {
                    continue;
                }

                yield return frames.Skip(index + 1)
                    .TakeWhile(frame => frame.FrameType == RenderTreeFrameType.Attribute)
                    .ToArray();
            }
        }

        IEnumerable<RenderTreeFrame> Frames(int id)
        {
            var frames = GetCurrentRenderTreeFrames(id);
            return frames.Array.Take(frames.Count);
        }
    }
#pragma warning restore BL0006
}

