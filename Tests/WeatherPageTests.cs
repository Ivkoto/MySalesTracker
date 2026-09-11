using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Models;
using MySalesTracker.Web.Components.Pages;
using MySalesTracker.Web.State;

namespace MySalesTracker.Tests;

public sealed class WeatherPageTests
{
    [Fact]
    public async Task NewSession_LoadsThreeDaysAutomatically()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake();
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();

            Assert.Equal(3, renderer.SelectedCount(page));
            Assert.Equal(3, renderer.TabCount(page));
            Assert.Equal([3], weather.RequestedDays);
            Assert.Equal(1, js.HourScrollCalls);
        });
    }

    [Fact]
    public async Task SelectingFiveDays_RestoresSelectionAndForecastAfterNavigation()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake();
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            await renderer.ChangeDaysAsync(page, 5);
            Assert.Equal(5, state.DisplayDays);
            Assert.Equal(5, renderer.TabCount(page));

            renderer.Close(page);
            var returnedPage = await renderer.OpenAsync();

            Assert.Equal(5, renderer.SelectedCount(returnedPage));
            Assert.Equal(5, renderer.TabCount(returnedPage));
            Assert.Equal([3, 5], weather.RequestedDays);
            Assert.Equal(3, new WeatherState().DisplayDays);
            Assert.Equal(3, js.HourScrollCalls);
        });
    }

    [Fact]
    public async Task FailedSelection_KeepsChosenCountAndRetriesItAfterNavigation()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        await using var services = CreateServices(state, weather);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            weather.FailNextForecast = true;
            await renderer.ChangeDaysAsync(page, 5);

            Assert.Equal(5, state.DisplayDays);
            Assert.Equal(0, renderer.TabCount(page));
            renderer.Close(page);
            var returnedPage = await renderer.OpenAsync();

            Assert.Equal(5, renderer.SelectedCount(returnedPage));
            Assert.Equal(5, renderer.TabCount(returnedPage));
            Assert.Equal([3, 5, 5], weather.RequestedDays);
        });
    }

    [Fact]
    public async Task OlderRequest_DoesNotOverwriteNewSelectionAfterNavigation()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        await using var services = CreateServices(state, weather);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            var pending = new TaskCompletionSource<WeatherForecast?>(TaskCreationOptions.RunContinuationsAsynchronously);
            weather.NextForecast = pending.Task;
            var olderRequest = renderer.ChangeDaysAsync(page, 5);

            Assert.Equal(5, state.DisplayDays);
            renderer.Close(page);
            var returnedPage = await renderer.OpenAsync();
            await renderer.ChangeDaysAsync(returnedPage, 7);

            pending.SetResult(WeatherServiceFake.CreateForecast(5));
            await olderRequest;

            Assert.Equal(7, state.DisplayDays);
            Assert.Equal(7, state.LastSummary!.Days.Count);
            Assert.Equal(7, renderer.SelectedCount(returnedPage));
            Assert.Equal(7, renderer.TabCount(returnedPage));
        });
    }

    [Fact]
    public async Task CurrentLocation_ReusesCoordinatesForDayChangesAndNavigation()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake();
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            Assert.Equal(0, js.Calls);
            await renderer.ChangeDaysAsync(page, 5);
            await renderer.UseLocationAsync(page);

            Assert.Equal(new WeatherState.Coordinates(45.123456, 12.345678), state.CurrentLocation);
            Assert.Equal("София", state.LastSummary!.Name);
            Assert.Equal("София", state.City);
            Assert.Equal("София", renderer.CityValue(page));
            Assert.Contains("София", renderer.Text(page));
            Assert.Equal(5, renderer.TabCount(page));
            await renderer.ChangeDaysAsync(page, 7);
            renderer.Close(page);
            var returnedPage = await renderer.OpenAsync();

            Assert.Equal(7, renderer.SelectedCount(returnedPage));
            Assert.Equal(7, renderer.TabCount(returnedPage));
            Assert.Equal("София", renderer.CityValue(returnedPage));
            Assert.Equal("София", state.LastSummary!.Name);
            Assert.Equal([3, 5, 5, 7], weather.RequestedDays);
            Assert.Equal((45.123456, 12.345678), weather.RequestedCoordinates[^1]);
            Assert.Equal(2, weather.RequestedCities.Count);
            Assert.Equal(1, js.Calls);
        });
    }

    [Theory]
    [InlineData("denied", "Достъпът до местоположението е отказан")]
    [InlineData("timeout", "отне твърде дълго")]
    [InlineData("unsupported", "Браузърът не поддържа")]
    [InlineData("unavailable", "Местоположението не е достъпно")]
    [InlineData("insecure", "HTTPS")]
    [InlineData("city-unavailable", "името на града не можа да бъде заредено")]
    public async Task LocationFailure_KeepsExistingForecast(string failure, string expectedMessage)
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake { Result = new Weather.LocationResult(0, 0, failure) };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            var original = state.LastSummary;
            await renderer.UseLocationAsync(page);

            Assert.Null(state.CurrentLocation);
            Assert.Same(original, state.LastSummary);
            Assert.Equal(3, renderer.TabCount(page));
            Assert.Equal([3], weather.RequestedDays);
            Assert.Contains(expectedMessage, renderer.Text(page));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BrowserRequestFailure_KeepsExistingForecast(bool canceled)
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake
        {
            PendingResult = Task.FromException<Weather.LocationResult>(canceled
                ? new OperationCanceledException()
                : new JSException("Browser request failed"))
        };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            var original = state.LastSummary;
            await renderer.UseLocationAsync(page);

            Assert.Null(state.CurrentLocation);
            Assert.Same(original, state.LastSummary);
            Assert.Equal(3, renderer.TabCount(page));
            Assert.Contains("въведете град", renderer.Text(page));
        });
    }

    [Fact]
    public async Task SearchingDetectedCity_KeepsExactDeviceCoordinates()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        await using var services = CreateServices(state, weather);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            await renderer.UseLocationAsync(page);
            await renderer.SearchCityAsync(page, "София");

            Assert.Equal(new WeatherState.Coordinates(45.123456, 12.345678), state.CurrentLocation);
            Assert.Equal((45.123456, 12.345678), weather.RequestedCoordinates[^1]);
            Assert.Single(weather.RequestedCities);
            Assert.Equal("София", renderer.CityValue(page));
        });
    }

    [Fact]
    public async Task LocationWithoutCity_KeepsPreviousSelection()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake { Result = new Weather.LocationResult(45.123456, 12.345678, null) };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            var original = state.LastSummary;
            await renderer.UseLocationAsync(page);

            Assert.Null(state.CurrentLocation);
            Assert.Equal("Sofia", renderer.CityValue(page));
            Assert.Same(original, state.LastSummary);
            Assert.Contains("името на града не можа да бъде заредено", renderer.Text(page));
        });
    }

    [Fact]
    public async Task CitySearch_AfterCurrentLocationSwitchesBackToCity()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake();
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            await renderer.UseLocationAsync(page);
            await renderer.SearchCityAsync(page, "Plovdiv");
            await renderer.ChangeDaysAsync(page, 5);

            Assert.Null(state.CurrentLocation);
            Assert.Equal("Plovdiv", state.City);
            Assert.Equal("Plovdiv", weather.RequestedCities[^1]);
            Assert.Equal((42.7, 23.3), weather.RequestedCoordinates[^1]);
            Assert.Equal(5, renderer.TabCount(page));
            Assert.Equal(1, js.Calls);
        });
    }

    [Fact]
    public async Task LocationResponse_AfterLeavingPageDoesNotChangeSession()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var pending = new TaskCompletionSource<Weather.LocationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var js = new LocationJsFake { PendingResult = pending.Task };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            var locating = renderer.UseLocationAsync(page);
            renderer.Close(page);
            var returnedPage = await renderer.OpenAsync();
            await renderer.SearchCityAsync(returnedPage, "Plovdiv");
            pending.SetResult(js.Result);
            await locating;

            Assert.Null(state.CurrentLocation);
            Assert.Equal("Plovdiv", state.City);
            Assert.Equal([3, 3], weather.RequestedDays);
        });
    }

    static ServiceProvider CreateServices(WeatherState state, WeatherServiceFake weather, LocationJsFake? js = null)
        => new ServiceCollection()
            .AddLogging()
            .AddSingleton(state)
            .AddSingleton<IWeatherService>(weather)
            .AddSingleton<IJSRuntime>(js ?? new LocationJsFake())
            .BuildServiceProvider();

    sealed class LocationJsFake : IJSRuntime
    {
        public Weather.LocationResult Result { get; set; } = new(45.123456, 12.345678, null, "София");
        public Task<Weather.LocationResult>? PendingResult { get; set; }
        public int Calls { get; private set; }
        public int HourScrollCalls { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "weatherForecast.showCurrentHour")
            {
                HourScrollCalls++;
                return default!;
            }

            Assert.Equal("weatherLocation.getCurrent", identifier);
            Calls++;
            var result = PendingResult is null ? Result : await PendingResult;
            return (TValue)(object)result;
        }
    }

    sealed class WeatherServiceFake : IWeatherService
    {
        public List<int> RequestedDays { get; } = [];
        public List<string> RequestedCities { get; } = [];
        public List<(double Lat, double Lon)> RequestedCoordinates { get; } = [];
        public bool FailNextForecast { get; set; }
        public Task<WeatherForecast?>? NextForecast { get; set; }

        public Task<(double lat, double lon, string? name)?> FetchGeocode(string city, string? language = "bg")
        {
            RequestedCities.Add(city);
            return Task.FromResult<(double, double, string?)?>((42.7, 23.3, "София"));
        }

        public Task<WeatherForecast?> GetForecast(double lat, double lon, int forecastDays = 7)
        {
            RequestedDays.Add(forecastDays);
            RequestedCoordinates.Add((lat, lon));
            if (NextForecast is { } pending)
            {
                NextForecast = null;
                return pending;
            }

            if (FailNextForecast)
            {
                FailNextForecast = false;
                return Task.FromResult<WeatherForecast?>(null);
            }

            return Task.FromResult<WeatherForecast?>(CreateForecast(forecastDays));
        }

        public static WeatherForecast CreateForecast(int forecastDays)
        {
            var start = new DateTime(2026, 9, 6);
            var hours = Enumerable.Range(0, forecastDays * 24)
                .Select(hour => new HourlyForecast(start.AddHours(hour), 20, 5, 25, 0.8))
                .ToList();
            return new WeatherForecast(hours);
        }
    }

    // Dispatch actual Blazor input events and recreate pages without an app server.
#pragma warning disable BL0006
    sealed class WeatherRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
        protected override void HandleException(Exception exception)
            => System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();

        public async Task<int> OpenAsync()
        {
            var id = AssignRootComponentId(InstantiateComponent(typeof(Weather)));
            await RenderRootComponentAsync(id);
            return id;
        }

        public void Close(int id) => RemoveRootComponent(id);

        public Task UseLocationAsync(int id)
        {
            var handler = ElementAttributes(id, "button", "location-button").Single(frame => frame.AttributeName == "onclick");
            return DispatchEventAsync(handler.AttributeEventHandlerId, null, new MouseEventArgs());
        }

        public async Task SearchCityAsync(int id, string city)
        {
            var input = ElementAttributes(id, "input").Single(frame => frame.AttributeName == "onchange");
            await DispatchEventAsync(input.AttributeEventHandlerId, null, new ChangeEventArgs { Value = city });
            var form = ElementAttributes(id, "form").Single(frame => frame.AttributeName == "onsubmit");
            await DispatchEventAsync(form.AttributeEventHandlerId, null, EventArgs.Empty);
        }

        public string Text(int id)
            => string.Join(" ", Frames(id).Where(frame => frame.FrameType == RenderTreeFrameType.Text).Select(frame => frame.TextContent));

        public Task ChangeDaysAsync(int id, int days)
        {
            var handler = SelectAttributes(id).Single(frame => frame.AttributeName == "onchange");
            return DispatchEventAsync(handler.AttributeEventHandlerId, null, new ChangeEventArgs { Value = days.ToString() });
        }

        public int SelectedCount(int id)
            => Convert.ToInt32(SelectAttributes(id).Single(frame => frame.AttributeName == "value").AttributeValue);

        public string CityValue(int id)
            => ElementAttributes(id, "input").Single(frame => frame.AttributeName == "value").AttributeValue.ToString()!;

        public int TabCount(int id)
            => Frames(id).Count(frame => frame.FrameType == RenderTreeFrameType.Attribute
                && frame.AttributeName == "role" && Equals(frame.AttributeValue, "tab"));

        IEnumerable<RenderTreeFrame> SelectAttributes(int id)
            => ElementAttributes(id, "select");

        IEnumerable<RenderTreeFrame> ElementAttributes(int id, string element, string? className = null)
        {
            var frames = Frames(id).ToArray();
            for (var i = 0; i < frames.Length; i++)
            {
                if (frames[i].FrameType != RenderTreeFrameType.Element || frames[i].ElementName != element)
                    continue;

                var attributes = frames.Skip(i + 1).TakeWhile(frame => frame.FrameType == RenderTreeFrameType.Attribute).ToArray();
                if (className is null || attributes.Any(frame => frame.AttributeName == "class"
                    && frame.AttributeValue.ToString()!.Split(' ').Contains(className)))
                    return attributes;
            }
            throw new InvalidOperationException($"Element not found: {element} {className}");
        }

        IEnumerable<RenderTreeFrame> Frames(int id)
        {
            var frames = GetCurrentRenderTreeFrames(id);
            return frames.Array.Take(frames.Count);
        }
    }
#pragma warning restore BL0006
}
