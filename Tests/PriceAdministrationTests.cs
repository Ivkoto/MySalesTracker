using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using MySalesTracker.Application.DTOs;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Application.Services;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;
using PricesPage = MySalesTracker.Web.Components.Pages.PriceAdministration;

namespace MySalesTracker.Tests;

public sealed class PriceAdministrationTests
{
    [Fact]
    public async Task FailedListLoad_ShowsError_AndPreventsSavingPreviousListPrices()
    {
        var repository = new ControlledProductRepository();
        repository.EnqueueSuccess(CreateProduct(Brand.Totem, PriceList.BG, 20m));
        repository.EnqueueFailure();

        await using var renderer = new PricesRenderer(CreateServices(repository));
        var id = await renderer.OpenAsync();

        Assert.Contains("20,00 €", renderer.Text(id));

        await renderer.ClickTabAsync(id, "EU");

        Assert.Contains("Цените не можаха да бъдат заредени.", renderer.Text(id));
        Assert.DoesNotContain("20,00 €", renderer.Text(id));
        Assert.False(renderer.HasButtonWithClass(id, "edit-price-button"));
        Assert.Empty(renderer.ReplacedRequests);
        await renderer.CloseAsync(id);
    }

    [Fact]
    public async Task LateLoadOfPreviousList_DoesNotOverwriteCurrentList_AndSaveUsesCurrentList()
    {
        var repository = new ControlledProductRepository();
        repository.EnqueueSuccess(CreateProduct(Brand.Totem, PriceList.BG, 20m));
        repository.EnqueueSuccess(CreateProduct(Brand.Totem, PriceList.EU, 30m));
        var pendingBgLoad = repository.EnqueuePending();
        repository.EnqueueSuccess(CreateProduct(Brand.Totem, PriceList.EU, 30m));

        await using var renderer = new PricesRenderer(CreateServices(repository));
        var id = await renderer.OpenAsync();

        await renderer.ClickTabAsync(id, "EU");
        Assert.Contains("30,00 €", renderer.Text(id));

        // BG load started, then the operator switches back to EU.
        var staleLoad = renderer.ClickTabAsync(id, "BG");
        await renderer.ClickTabAsync(id, "EU");
        Assert.Contains("30,00 €", renderer.Text(id));

        pendingBgLoad.SetResult([CreateProduct(Brand.Totem, PriceList.BG, 20m)]);
        await staleLoad;

        Assert.Contains("30,00 €", renderer.Text(id));
        Assert.DoesNotContain("20,00 €", renderer.Text(id));

        await renderer.ClickByClassAsync(id, "edit-price-button");
        await renderer.ClickByClassAsync(id, "btn-primary", "btn-sm");

        var request = Assert.Single(renderer.ReplacedRequests);
        Assert.Equal(PriceList.EU, request.PriceList);
        Assert.Equal(30m, Assert.Single(request.Rules).Price);
        await renderer.CloseAsync(id);
    }

    [Fact]
    public async Task StaleEditClick_FromPreviousList_CannotOpenEditor()
    {
        var repository = new ControlledProductRepository();
        repository.EnqueueSuccess(CreateProduct(Brand.Totem, PriceList.BG, 20m));
        var euLoad = repository.EnqueuePending();

        await using var renderer = new PricesRenderer(CreateServices(repository));
        var id = await renderer.OpenAsync();

        Assert.Contains("20,00 €", renderer.Text(id));
        var staleEditHandler = renderer.EditHandlerId(id);
        Assert.NotNull(staleEditHandler);

        // The operator taps EU; the load is still in flight when the old card is tapped again.
        var euClick = renderer.ClickTabAsync(id, "EU");
        await renderer.InvokeHandlerAsync(staleEditHandler!.Value);

        Assert.False(renderer.HasPriceEditor(id));

        euLoad.SetResult([CreateProduct(Brand.Totem, PriceList.EU, 30m)]);
        await euClick;

        Assert.Contains("30,00 €", renderer.Text(id));

        // The stale card object itself (BG 20 €) must not open a draft while EU is displayed.
        await renderer.TryStartEditingAsync(id, CreatePricing(PriceList.BG, 20m));
        Assert.False(renderer.HasPriceEditor(id));

        await renderer.ClickByClassAsync(id, "edit-price-button");
        Assert.True(renderer.HasPriceEditor(id));
        Assert.Equal("30,00", renderer.PriceEditorValue(id));
        await renderer.ClickByClassAsync(id, "btn-primary", "btn-sm");

        var request = Assert.Single(renderer.ReplacedRequests);
        Assert.Equal(PriceList.EU, request.PriceList);
        Assert.Equal(30m, Assert.Single(request.Rules).Price);
        await renderer.CloseAsync(id);
    }

    [Fact]
    public async Task PendingSaveOfPreviousList_DoesNotDiscardNewerDraft()
    {
        var repository = new ControlledProductRepository();
        repository.EnqueueSuccess(CreateProduct(Brand.Totem, PriceList.BG, 20m));
        repository.EnqueueSuccess(CreateProduct(Brand.Totem, PriceList.EU, 30m));

        var bgWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var renderer = new PricesRenderer(CreateServices(repository));
        renderer.PendingWrites.Enqueue(() => bgWrite.Task);

        var id = await renderer.OpenAsync();

        await renderer.ClickByClassAsync(id, "edit-price-button");
        var bgSave = renderer.ClickByClassAsync(id, "btn-primary", "btn-sm");

        await renderer.ClickTabAsync(id, "EU");
        Assert.Contains("30,00 €", renderer.Text(id));

        await renderer.ClickByClassAsync(id, "edit-price-button");
        await renderer.TypePriceAsync(id, "35");
        Assert.Equal("35", renderer.PriceEditorValue(id));

        bgWrite.SetResult();
        await bgSave;

        Assert.Equal("35", renderer.PriceEditorValue(id));
        Assert.Single(renderer.ReplacedRequests);

        await renderer.ClickByClassAsync(id, "btn-primary", "btn-sm");

        Assert.Collection(
            renderer.ReplacedRequests,
            first =>
            {
                Assert.Equal(PriceList.BG, first.PriceList);
                Assert.Equal(20m, Assert.Single(first.Rules).Price);
            },
            second =>
            {
                Assert.Equal(PriceList.EU, second.PriceList);
                Assert.Equal(35m, Assert.Single(second.Rules).Price);
            });
        await renderer.CloseAsync(id);
    }

    static ProductPricing CreatePricing(PriceList priceList, decimal price)
        => new(
            7,
            "Свещ",
            Brand.Totem,
            false,
            [new PriceRuleDetails(price, Currency.EUR, 1, 1, true)]);

    [Fact]
    public async Task SecondProductOnSameTab_HasEnabledButtons_AfterEarlierSaveCompletes()
    {
        var repository = new ControlledProductRepository();
        repository.EnqueueSuccess(
            CreateProduct(1, "Свещ", Brand.Totem, PriceList.BG, 20m),
            CreateProduct(2, "Купа", Brand.Totem, PriceList.BG, 30m));

        var productAWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var renderer = new PricesRenderer(CreateServices(repository));
        renderer.PendingWrites.Enqueue(() => productAWrite.Task);

        var id = await renderer.OpenAsync();

        // Product A starts saving, its write stays pending.
        await renderer.ClickByClassAsync(id, "edit-price-button");
        var saveA = renderer.ClickByClassAsync(id, "btn-primary", "btn-sm");

        // Product B opens in the same tab while A's write is still pending.
        await renderer.ClickByClassAsync(id, "edit-price-button");
        await renderer.TypePriceAsync(id, "35");
        Assert.Equal("35", renderer.PriceEditorValue(id));

        productAWrite.SetResult();
        await saveA;

        Assert.Equal("35", renderer.PriceEditorValue(id));
        Assert.True(renderer.IsButtonEnabled(id, "btn-primary", "btn-sm"));
        Assert.True(renderer.IsButtonEnabled(id, "btn-outline-secondary", "btn-sm"));
        Assert.Single(renderer.ReplacedRequests);

        await renderer.ClickByClassAsync(id, "btn-primary", "btn-sm");

        Assert.Collection(
            renderer.ReplacedRequests,
            first =>
            {
                Assert.Equal(1, first.ProductId);
                Assert.Equal(PriceList.BG, first.PriceList);
                Assert.Equal(20m, Assert.Single(first.Rules).Price);
            },
            second =>
            {
                Assert.Equal(2, second.ProductId);
                Assert.Equal(PriceList.BG, second.PriceList);
                Assert.Equal(35m, Assert.Single(second.Rules).Price);
            });
        await renderer.CloseAsync(id);
    }

    [Fact]
    public async Task SavedProductOfEarlierSession_ShowsNewPrice_WhenReopenedDuringAnotherDraft()
    {
        var repository = new ControlledProductRepository();
        repository.EnqueueSuccess(
            CreateProduct(1, "Свещ", Brand.Totem, PriceList.BG, 20m),
            CreateProduct(2, "Купа", Brand.Totem, PriceList.BG, 30m));

        var productAWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var renderer = new PricesRenderer(CreateServices(repository));
        renderer.PendingWrites.Enqueue(() => productAWrite.Task);

        var id = await renderer.OpenAsync();

        // A: 20 → 25, write held pending.
        await renderer.ClickByClassAsync(id, "edit-price-button");
        await renderer.TypePriceAsync(id, "25");
        var saveA = renderer.ClickByClassAsync(id, "btn-primary", "btn-sm");

        // B draft starts while A is still saving.
        await renderer.ClickByClassAsync(id, "edit-price-button");
        await renderer.TypePriceAsync(id, "35");

        productAWrite.SetResult();
        await saveA;

        Assert.Contains("25,00 €", renderer.Text(id));
        Assert.DoesNotContain("20,00 €", renderer.Text(id));
        Assert.Equal("35", renderer.PriceEditorValue(id));
        Assert.True(renderer.IsButtonEnabled(id, "btn-primary", "btn-sm"));
        Assert.True(renderer.IsButtonEnabled(id, "btn-outline-secondary", "btn-sm"));

        var onlyWrite = Assert.Single(renderer.ReplacedRequests);
        Assert.Equal(1, onlyWrite.ProductId);
        Assert.Equal(PriceList.BG, onlyWrite.PriceList);
        Assert.Equal(25m, Assert.Single(onlyWrite.Rules).Price);

        // Cancel B and reopen A before saving B, so B's save cannot mask the stale price.
        await renderer.ClickByClassAsync(id, "btn-outline-secondary", "btn-sm");
        await renderer.ClickByClassAsync(id, "edit-price-button");

        Assert.Equal("25,00", renderer.PriceEditorValue(id));
        Assert.Single(renderer.ReplacedRequests);
        await renderer.CloseAsync(id);
    }

    static Product CreateProduct(Brand brand, PriceList priceList, decimal price)
        => CreateProduct(7, "Свещ", brand, priceList, price);

    static Product CreateProduct(int productId, string name, Brand brand, PriceList priceList, decimal price)
        => new()
        {
            ProductId = productId,
            Name = name,
            Brand = brand,
            PriceRules =
            [
                new PriceRule
                {
                    ProductId = productId,
                    PriceList = priceList,
                    Price = price,
                    Currency = Currency.EUR,
                    UnitsPerSale = 1,
                    SortOrder = 1,
                    IsDefault = true
                }
            ]
        };

    static ServiceProvider CreateServices(ControlledProductRepository productRepository)
    {
        var priceRuleRepository = new RecordingPriceRuleRepository();
        return new ServiceCollection()
            .AddLogging()
            .AddSingleton(new ProductService(productRepository, NullLogger<ProductService>.Instance))
            .AddSingleton(new PriceRuleService(priceRuleRepository, productRepository, NullLogger<PriceRuleService>.Instance))
            .AddSingleton<INotificationService>(new NoOpNotificationService())
            .AddSingleton<IJSRuntime>(new ConfirmJs())
            .AddSingleton<PriceRuleRepositoryAccessor>(new PriceRuleRepositoryAccessor(priceRuleRepository))
            .BuildServiceProvider();
    }

    sealed class PriceRuleRepositoryAccessor(RecordingPriceRuleRepository repository)
    {
        public RecordingPriceRuleRepository Repository { get; } = repository;
    }

    sealed class TestPricesPage : PricesPage
    {
        public void TryStartEditing(ProductPricing product)
        {
            StartEditing(product);
            StateHasChanged();
        }
    }

    sealed class ControlledProductRepository : IProductRepository
    {
        readonly Queue<Func<Task<List<Product>>>> loads = new();
        List<Product> lastLoaded = [];

        public void EnqueueSuccess(params Product[] products)
            => loads.Enqueue(() => Task.FromResult(products.ToList()));

        public void EnqueueFailure()
            => loads.Enqueue(() => Task.FromException<List<Product>>(new InvalidOperationException("load failed")));

        public TaskCompletionSource<List<Product>> EnqueuePending()
        {
            var completion = new TaskCompletionSource<List<Product>>(TaskCreationOptions.RunContinuationsAsynchronously);
            loads.Enqueue(() => completion.Task);
            return completion;
        }

        public Task<bool> TryAddAsync(Product product, CancellationToken ct) => Task.FromResult(false);

        public Task<List<Product>> GetActiveProductsAsync(CancellationToken ct)
            => GetActiveProductsWithPriceRulesAsync(ct);

        public async Task<List<Product>> GetActiveProductsWithPriceRulesAsync(CancellationToken ct)
        {
            if (loads.Count == 0)
            {
                throw new InvalidOperationException("No load response queued.");
            }

            var products = await loads.Dequeue()();
            lastLoaded = products;
            return products;
        }

        public Task<Product?> GetActiveProductWithPriceRulesAsync(int productId, CancellationToken ct)
            => Task.FromResult(lastLoaded.FirstOrDefault(product => product.ProductId == productId));
    }

    sealed class RecordingPriceRuleRepository : IPriceRuleRepository
    {
        public List<UpdateProductPriceRulesRequest> Replacements { get; } = [];

        public Queue<Func<Task>> PendingWrites { get; } = new();

        public Task<PriceRule?> GetRuleForProductAsync(int productId, PriceList priceList, decimal price, CancellationToken ct)
            => Task.FromResult<PriceRule?>(null);

        public Task<List<PriceRule>> GetAllPriceRulesAsync(PriceList priceList, CancellationToken ct)
            => Task.FromResult(new List<PriceRule>());

        public Task ReplacePriceRulesAsync(int productId, PriceList priceList, IReadOnlyCollection<PriceRule> priceRules, CancellationToken ct)
        {
            Replacements.Add(new UpdateProductPriceRulesRequest(
                productId,
                priceList,
                [.. priceRules.Select(rule => new PriceRuleInput(rule.Price, rule.UnitsPerSale, rule.IsDefault))]));

            return PendingWrites.Count > 0 ? PendingWrites.Dequeue()() : Task.CompletedTask;
        }
    }

    sealed class NoOpNotificationService : INotificationService
    {
        public Task NotifySaleCreatedAsync(int eventDayId, int saleId, CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifyEventCreatedAsync(int eventId, CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifyProductCreatedAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    sealed class ConfirmJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => ValueTask.FromResult((TValue)(object)true);
    }

    sealed class AcknowledgementHold
    {
        readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Pending => completion.Task;

        public void Release() => completion.TrySetResult();
    }

#pragma warning disable BL0006
    sealed class PricesRenderer : Renderer
    {
        readonly RecordingPriceRuleRepository priceRuleRepository;
        AcknowledgementHold? acknowledgementHold;

        public PricesRenderer(IServiceProvider services) : base(services, NullLoggerFactory.Instance)
        {
            priceRuleRepository = services.GetRequiredService<PriceRuleRepositoryAccessor>().Repository;
        }

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public IReadOnlyList<UpdateProductPriceRulesRequest> ReplacedRequests => priceRuleRepository.Replacements;

        public Queue<Func<Task>> PendingWrites => priceRuleRepository.PendingWrites;

        public AcknowledgementHold HoldAcknowledgement()
        {
            acknowledgementHold = new AcknowledgementHold();
            return acknowledgementHold;
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
            => acknowledgementHold?.Pending ?? Task.CompletedTask;

        protected override void HandleException(Exception exception)
            => ExceptionDispatchInfo.Capture(exception).Throw();

        public Task<int> OpenAsync()
            => Dispatcher.InvokeAsync(async () =>
            {
                var id = AssignRootComponentId(InstantiateComponent(typeof(TestPricesPage)));
                await RenderRootComponentAsync(id, ParameterView.Empty);
                return id;
            });

        public Task TryStartEditingAsync(int id, ProductPricing product)
            => Dispatcher.InvokeAsync(() => ((TestPricesPage)GetComponentState(id).Component).TryStartEditing(product));

        public Task CloseAsync(int id)
            => Dispatcher.InvokeAsync(() => RemoveRootComponent(id));

        public string Text(int id)
            => string.Join(" ", Frames(id)
                .Where(frame => frame.FrameType == RenderTreeFrameType.Text)
                .Select(frame => frame.TextContent));

        public ulong? EditHandlerId(int id)
            => HandlerIdOf(id, "edit-price-button");

        public Task<bool> InvokeHandlerAsync(ulong handlerId)
            => Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await DispatchEventAsync(handlerId, null, new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
                    return true;
                }
                catch (ArgumentException)
                {
                    // The old card is gone, so the handler was already released.
                    return false;
                }
            });

        public bool HasPriceEditor(int id)
            => FindInputs(id, "Цена").Any();

        public string? PriceEditorValue(int id)
        {
            var attributes = FindInputs(id, "Цена").FirstOrDefault();
            return attributes is null ? null : Attribute(attributes, "value");
        }

        public Task TypePriceAsync(int id, string value)
            => Dispatcher.InvokeAsync(() =>
            {
                var attributes = FindInputs(id, "Цена").FirstOrDefault()
                    ?? throw new InvalidOperationException("Price editor is not open.");
                var handler = attributes.FirstOrDefault(frame => frame.AttributeName == "oninput");
                return DispatchEventAsync(handler.AttributeEventHandlerId, null, new ChangeEventArgs { Value = value });
            });

        public Task ClickTabAsync(int id, string label)
            => ClickButtonAsync(id, label);

        public bool HasButtonWithClass(int id, string className)
            => FindButtons(id)
                .Any(attributes => Attribute(attributes, "class")?.Split(' ').Contains(className) == true);

        public bool IsButtonEnabled(int id, params string[] classNames)
        {
            var attributes = FindButtons(id).FirstOrDefault(candidate =>
            {
                var classes = Attribute(candidate, "class")?.Split(' ') ?? [];
                return classNames.All(className => classes.Contains(className));
            }) ?? throw new InvalidOperationException($"Button not found by class: {string.Join('.', classNames)}");

            return !attributes.Any(attribute => attribute.AttributeName == "disabled");
        }

        public Task ClickByClassAsync(int id, params string[] classNames)
            => Dispatcher.InvokeAsync(() =>
            {
                var attributes = FindButtons(id).FirstOrDefault(candidate =>
                {
                    var classes = Attribute(candidate, "class")?.Split(' ') ?? [];
                    return classNames.All(className => classes.Contains(className));
                }) ?? throw new InvalidOperationException($"Button not found by class: {string.Join('.', classNames)}");

                return DispatchEventAsync(
                    attributes.Single(attribute => attribute.AttributeName == "onclick").AttributeEventHandlerId,
                    null,
                    new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            });

        public Task ClickButtonAsync(int id, string label)
            => Dispatcher.InvokeAsync(() =>
            {
                var frame = FindButton(id, label);
                var handler = frame.Single(attribute => attribute.AttributeName == "onclick");
                return DispatchEventAsync(handler.AttributeEventHandlerId, null, new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
            });

        ulong? HandlerIdOf(int id, string className)
        {
            var attributes = FindButtons(id).FirstOrDefault(candidate =>
                Attribute(candidate, "class")?.Split(' ').Contains(className) == true);
            return attributes is null
                ? null
                : attributes.FirstOrDefault(frame => frame.AttributeName == "onclick").AttributeEventHandlerId;
        }

        IEnumerable<RenderTreeFrame[]> FindInputs(int id, string ariaLabel)
        {
            var frames = Frames(id).ToArray();
            for (var index = 0; index < frames.Length; index++)
            {
                if (frames[index].FrameType != RenderTreeFrameType.Element || frames[index].ElementName != "input")
                {
                    continue;
                }

                var attributes = frames.Skip(index + 1)
                    .TakeWhile(frame => frame.FrameType == RenderTreeFrameType.Attribute)
                    .ToArray();
                if (Attribute(attributes, "aria-label") == ariaLabel)
                {
                    yield return attributes;
                }
            }
        }

        IEnumerable<RenderTreeFrame[]> FindButtons(int id)
        {
            var frames = Frames(id).ToArray();
            for (var index = 0; index < frames.Length; index++)
            {
                if (frames[index].FrameType != RenderTreeFrameType.Element || frames[index].ElementName != "button")
                {
                    continue;
                }

                yield return frames.Skip(index + 1)
                    .TakeWhile(frame => frame.FrameType == RenderTreeFrameType.Attribute)
                    .ToArray();
            }
        }

        static string? Attribute(RenderTreeFrame[] attributes, string name)
            => attributes.FirstOrDefault(frame => frame.AttributeName == name).AttributeValue?.ToString();

        RenderTreeFrame[] FindButton(int id, string label)
        {
            var frames = Frames(id).ToArray();
            var labels = new List<string>();
            for (var index = 0; index < frames.Length; index++)
            {
                if (frames[index].FrameType != RenderTreeFrameType.Element || frames[index].ElementName != "button")
                {
                    continue;
                }

                var attributes = frames.Skip(index + 1)
                    .TakeWhile(frame => frame.FrameType == RenderTreeFrameType.Attribute)
                    .ToArray();
                var subtree = frames.Skip(index + 1).Take(frames[index].ElementSubtreeLength - 1).ToArray();
                var text = string.Join(" ", subtree
                    .Where(frame => frame.FrameType == RenderTreeFrameType.Text)
                    .Select(frame => frame.TextContent))
                    .Trim();

                labels.Add(text);
                if (text == label)
                {
                    return attributes;
                }
            }

            throw new InvalidOperationException(
                $"Button not found: {label}. Buttons: [{string.Join(" | ", labels)}]. Text: {Text(id)}");
        }

        IEnumerable<RenderTreeFrame> Frames(int id)
        {
            var frames = GetCurrentRenderTreeFrames(id);
            return frames.Array.Take(frames.Count);
        }
    }
#pragma warning restore BL0006
}
