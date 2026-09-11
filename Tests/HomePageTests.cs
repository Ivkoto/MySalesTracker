using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Models;
using MySalesTracker.Web.Components.Pages;
using MySalesTracker.Web.State;

namespace MySalesTracker.Tests;

public sealed class HomePageTests
{
    [Fact]
    public async Task NewSession_ShowsCurrentRainAndDayTemperatures()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        await using var services = CreateServices(state, weather);
        await using var renderer = new HomeRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            var text = renderer.Text(page);

            Assert.Contains("София", text);
            Assert.Contains("Вали в момента", text);
            Assert.Contains("24°", text);
            Assert.Contains("18°", text);
            Assert.Contains("30°", text);
            Assert.Contains("5 km/h", text);
            Assert.Equal(["events", "weather"], renderer.RootAttributeValues(page, "href"));
            Assert.Equal(["Sofia"], weather.RequestedCities);
            Assert.Equal([1], weather.RequestedDays);
        });
    }

    [Fact]
    public async Task CachedForecast_ShowsNextRainWithoutAnotherRequest()
    {
        var state = new WeatherState { City = "Plovdiv" };
        state.Store(new WeatherState.Summary(
            "Пловдив",
            42.1,
            24.7,
            [new WeatherState.DaySummary(
                new DateOnly(2026, 9, 11),
                CreateHours()
                    .Select(hour => new WeatherState.HourEntry(
                        hour.Time,
                        hour.Temperature,
                        hour.WindSpeed,
                        hour.PrecipitationProbability,
                        hour.Time.Hour == 17 ? 0.8 : 0))
                    .ToList())],
            new WeatherState.CurrentCondition(new DateTime(2026, 9, 11, 10, 0, 0), 20, 0.8)));
        var weather = new WeatherServiceFake();
        await using var services = CreateServices(state, weather);
        await using var renderer = new HomeRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            var text = renderer.Text(page);

            Assert.Contains("Пловдив", text);
            Assert.Contains("Дъжд около 17:00", text);
            Assert.DoesNotContain("Вали в момента", text);
            Assert.Empty(weather.RequestedCities);
            Assert.Empty(weather.RequestedDays);
        });
    }

    static ServiceProvider CreateServices(WeatherState state, WeatherServiceFake weather)
        => new ServiceCollection()
            .AddLogging()
            .AddSingleton(state)
            .AddSingleton<IWeatherService>(weather)
            .AddSingleton<IJSRuntime>(new CurrentHourJsFake())
            .AddSingleton<NavigationManager>(new TestNavigationManager())
            .BuildServiceProvider();

    static List<HourlyForecast> CreateHours()
        => Enumerable.Range(0, 24)
            .Select(hour => new HourlyForecast(
                new DateTime(2026, 9, 11, hour, 0, 0),
                10 + hour,
                5,
                hour >= 14 ? 60 : 10,
                hour == 14 ? 0.4 : 0))
            .ToList();

    sealed class WeatherServiceFake : IWeatherService
    {
        public List<string> RequestedCities { get; } = [];
        public List<int> RequestedDays { get; } = [];

        public Task<(double lat, double lon, string? name)?> FetchGeocode(string city, string? language = "bg")
        {
            RequestedCities.Add(city);
            return Task.FromResult<(double, double, string?)?>((42.7, 23.3, "София"));
        }

        public Task<WeatherForecast?> GetForecast(double lat, double lon, int forecastDays = 7)
        {
            RequestedDays.Add(forecastDays);
            return Task.FromResult<WeatherForecast?>(new WeatherForecast(
                CreateHours(),
                new CurrentWeather(new DateTime(2026, 9, 11, 14, 0, 0), 24, 0.4)));
        }
    }

    sealed class CurrentHourJsFake : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Assert.Equal("weatherForecast.getCurrentHourKey", identifier);
            return ValueTask.FromResult((TValue)(object)"2026-09-11T14");
        }
    }

    sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");
    }

#pragma warning disable BL0006
    sealed class HomeRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
        protected override void HandleException(Exception exception)
            => System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();

        public async Task<int> OpenAsync()
        {
            var id = AssignRootComponentId(InstantiateComponent(typeof(Home)));
            await RenderRootComponentAsync(id);
            return id;
        }

        public string Text(int id)
            => string.Join(" ", Frames(id)
                .Where(frame => frame.FrameType == RenderTreeFrameType.Text)
                .Select(frame => frame.TextContent));

        public IReadOnlyList<string> AttributeValues(int id, string name)
            => Frames(id)
                .Where(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeName == name)
                .Select(frame => frame.AttributeValue?.ToString())
                .OfType<string>()
                .ToList();

        public IReadOnlyList<string> RootAttributeValues(int id, string name)
        {
            var frames = GetCurrentRenderTreeFrames(id);
            return frames.Array
                .Take(frames.Count)
                .Where(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeName == name)
                .Select(frame => frame.AttributeValue?.ToString())
                .OfType<string>()
                .ToList();
        }

        IEnumerable<RenderTreeFrame> Frames(int id)
        {
            var frames = GetCurrentRenderTreeFrames(id);
            for (var index = 0; index < frames.Count; index++)
            {
                var frame = frames.Array[index];
                yield return frame;

                if (frame.FrameType != RenderTreeFrameType.Component)
                {
                    continue;
                }

                foreach (var childFrame in Frames(frame.ComponentId))
                {
                    yield return childFrame;
                }
            }
        }
    }
#pragma warning restore BL0006
}
