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
            Assert.Equal([(45.123456, 12.345678)], weather.RequestedCoordinates);
            Assert.Empty(weather.RequestedCities);
            Assert.Equal(new WeatherState.Coordinates(45.123456, 12.345678), state.CurrentLocation);
            Assert.Equal("София", state.City);
            Assert.Equal("София", renderer.CityValue(page));
            Assert.Equal(1, js.Calls);
            Assert.Equal(1, js.HourScrollCalls);
            Assert.Equal(["2026-09-06T14"], js.HourKeys);
            Assert.Equal(["Europe/Sofia"], js.HourTimeZones);
        });
    }

    [Fact]
    public async Task FirstVisit_DeviceLocationWinsOverSavedCity()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake { SavedCity = "Plovdiv" };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();

            Assert.Equal(1, js.Calls);
            Assert.Equal(0, js.SavedCityReads);
            Assert.Equal(new WeatherState.Coordinates(45.123456, 12.345678), state.CurrentLocation);
            Assert.Equal("София", state.City);
            Assert.Empty(weather.RequestedCities);
            Assert.Equal([(45.123456, 12.345678)], weather.RequestedCoordinates);
            Assert.Equal("София", renderer.CityValue(page));
        });
    }

    [Fact]
    public async Task DeniedLocation_UsesSavedCity()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake
        {
            Result = new Weather.LocationResult(0, 0, "denied"),
            SavedCity = "Plovdiv"
        };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();

            Assert.Equal(1, js.Calls);
            Assert.Equal(1, js.SavedCityReads);
            Assert.Null(state.CurrentLocation);
            Assert.Equal("Plovdiv", state.City);
            Assert.Equal(["Plovdiv"], weather.RequestedCities);
            Assert.Equal("Plovdiv", renderer.CityValue(page));
            Assert.Equal(3, renderer.TabCount(page));
            Assert.DoesNotContain("Достъпът до местоположението е отказан", renderer.Text(page));

            renderer.Close(page);
            var returnedPage = await renderer.OpenAsync();

            Assert.Equal(["Plovdiv"], weather.RequestedCities);
            Assert.Equal(1, js.SavedCityReads);
            Assert.Equal("Plovdiv", renderer.CityValue(returnedPage));
            Assert.Equal(3, renderer.TabCount(returnedPage));
        });
    }

    [Fact]
    public async Task FreshBrowser_DeniedLocation_UsesSofiaDefault()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake { Result = new Weather.LocationResult(0, 0, "denied") };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();

            Assert.Null(state.CurrentLocation);
            Assert.Equal("Sofia", state.City);
            Assert.Equal(["Sofia"], weather.RequestedCities);
            Assert.Equal("Sofia", renderer.CityValue(page));
            Assert.Equal(3, renderer.TabCount(page));
            Assert.DoesNotContain("Достъпът до местоположението е отказан", renderer.Text(page));
        });
    }

    [Fact]
    public async Task CachedForecast_DoesNotSkipDeviceLocationCheck()
    {
        var state = new WeatherState();
        state.Store(CreateStoredSummary("Sofia"));
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake();
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();

            Assert.Equal(1, js.Calls);
            Assert.Equal(new WeatherState.Coordinates(45.123456, 12.345678), state.CurrentLocation);
            Assert.Equal("София", state.City);
            Assert.Equal([(45.123456, 12.345678)], weather.RequestedCoordinates);
            Assert.Equal(3, renderer.TabCount(page));
        });
    }

    [Fact]
    public async Task ManualSearch_IsSavedAndWinsOverLateLocationResponse()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake();
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            var pending = new TaskCompletionSource<Weather.LocationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            js.PendingResult = pending.Task;
            var locating = renderer.UseLocationAsync(page);

            await renderer.SearchCityAsync(page, "Plovdiv");
            pending.SetResult(new Weather.LocationResult(45.123456, 12.345678, null, "София"));
            await locating;

            Assert.Null(state.CurrentLocation);
            Assert.Equal("Plovdiv", state.City);
            Assert.Equal("Plovdiv", renderer.CityValue(page));
            Assert.Equal(["Plovdiv"], js.SavedCities);
            Assert.Equal("Plovdiv", weather.RequestedCities[^1]);
        });
    }

    [Fact]
    public async Task ManualSearch_IsRememberedForNextSession()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake();
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            await renderer.SearchCityAsync(page, "Plovdiv");

            Assert.Equal(["Plovdiv"], js.SavedCities);
        });

        js.SavedCity = "Plovdiv";
        js.Result = new Weather.LocationResult(0, 0, "denied");
        var nextState = new WeatherState();
        var nextWeather = new WeatherServiceFake();
        await using var nextServices = CreateServices(nextState, nextWeather, js);
        await using var nextRenderer = new WeatherRenderer(nextServices);

        await nextRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await nextRenderer.OpenAsync();

            Assert.Equal("Plovdiv", nextState.City);
            Assert.Equal(["Plovdiv"], nextWeather.RequestedCities);
            Assert.Equal("Plovdiv", nextRenderer.CityValue(page));
        });
    }

    [Fact]
    public async Task FailedManualSearch_KeepsLastSuccessfulCity()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake();
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            await renderer.SearchCityAsync(page, "Plovdiv");
            weather.FailNextGeocode = true;
            await renderer.SearchCityAsync(page, "Varna");

            Assert.Contains("Градът не е намерен.", renderer.Text(page));
            Assert.Equal("Plovdiv", state.City);
            Assert.Equal("Plovdiv", renderer.CityValue(page));
            Assert.Equal("Plovdiv", state.LastSummary!.Name);
            Assert.Equal(["Plovdiv"], js.SavedCities);
            Assert.Equal(3, renderer.TabCount(page));
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
            Assert.Equal(1, js.Calls);
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
            Assert.Empty(weather.RequestedCities);
            Assert.Equal(2, js.Calls);
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
        var js = new LocationJsFake();
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            await renderer.UseLocationAsync(page);
            await renderer.SearchCityAsync(page, "София");

            Assert.Equal(new WeatherState.Coordinates(45.123456, 12.345678), state.CurrentLocation);
            Assert.Equal((45.123456, 12.345678), weather.RequestedCoordinates[^1]);
            Assert.Empty(weather.RequestedCities);
            Assert.Equal(["София"], js.SavedCities);
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
            Assert.Equal(["Plovdiv"], js.SavedCities);
            Assert.Equal(5, renderer.TabCount(page));
            Assert.Equal(2, js.Calls);
        });
    }

    [Fact]
    public async Task LocationResponse_AfterLeavingPageDoesNotChangeSession()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var pending = new TaskCompletionSource<Weather.LocationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var js = new LocationJsFake();
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            js.PendingResult = pending.Task;
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

    [Fact]
    public async Task FailedManualSearch_LeavesStartupLookupRunning()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var pending = new TaskCompletionSource<Weather.LocationResult>();
        var js = new LocationJsFake { PendingResult = pending.Task };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var (page, rendering) = renderer.BeginOpen();
            await WaitForAsync(() => js.Calls == 1);

            weather.FailNextGeocode = true;
            await renderer.SearchCityAsync(page, "Непознат");

            Assert.Contains("Градът не е намерен.", renderer.Text(page));
            Assert.Null(state.CurrentLocation);
            Assert.DoesNotContain("Непознат", js.SavedCities);

            // The startup lookup must still be alive and apply the device location.
            pending.SetResult(new Weather.LocationResult(45.123456, 12.345678, null, "София"));
            await rendering;

            Assert.Equal(new WeatherState.Coordinates(45.123456, 12.345678), state.CurrentLocation);
            Assert.Equal("София", state.City);
            Assert.Equal("София", renderer.CityValue(page));
            Assert.Contains((45.123456, 12.345678), weather.RequestedCoordinates);
            Assert.Equal(3, renderer.TabCount(page));
            renderer.Close(page);
        });
    }

    [Fact]
    public async Task SuccessfulManualSearch_WinsOverLateAutomaticLocation()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var pending = new TaskCompletionSource<Weather.LocationResult>();
        var js = new LocationJsFake { PendingResult = pending.Task };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var (page, rendering) = renderer.BeginOpen();
            await WaitForAsync(() => js.Calls == 1);

            await renderer.SearchCityAsync(page, "Plovdiv");

            Assert.Equal("Plovdiv", state.City);
            Assert.Equal(["Plovdiv"], js.SavedCities);

            // The late automatic result must not take the selection back.
            pending.SetResult(new Weather.LocationResult(45.123456, 12.345678, null, "София"));
            await rendering;

            Assert.Null(state.CurrentLocation);
            Assert.Equal("Plovdiv", state.City);
            Assert.Equal("Plovdiv", renderer.CityValue(page));
            Assert.DoesNotContain((45.123456, 12.345678), weather.RequestedCoordinates);
            Assert.Equal(3, renderer.TabCount(page));
            renderer.Close(page);
        });
    }

    [Fact]
    public async Task LeavingFirstDeviceLookup_NextVisitFinishesLocationSetup()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var pending = new TaskCompletionSource<Weather.LocationResult>();
        var js = new LocationJsFake { PendingResult = pending.Task };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var (page, _) = renderer.BeginOpen();
            await WaitForAsync(() => js.Calls == 1);
            renderer.Close(page);

            // Setup never completed, so the next visit must retry the whole lookup order.
            Assert.False(state.LocationAttempted);

            js.PendingResult = null;
            js.Result = new Weather.LocationResult(42.1, 24.7, null, "Варна");
            var returnedPage = await renderer.OpenAsync();

            Assert.True(state.LocationAttempted);
            Assert.Equal(2, js.Calls);
            Assert.Equal("Варна", state.City);
            Assert.Equal(new WeatherState.Coordinates(42.1, 24.7), state.CurrentLocation);
            Assert.Equal((42.1, 24.7), weather.RequestedCoordinates[^1]);
            Assert.Equal("Варна", renderer.CityValue(returnedPage));
            Assert.Equal(3, renderer.TabCount(returnedPage));

            // The abandoned page must ignore its late result.
            pending.SetResult(new Weather.LocationResult(45.123456, 12.345678, null, "Пловдив"));
            await Task.Delay(100);

            Assert.Equal("Варна", state.City);
            Assert.Equal("Варна", state.LastSummary!.Name);
            Assert.DoesNotContain((45.123456, 12.345678), weather.RequestedCoordinates);
            renderer.Close(returnedPage);
        });
    }

    [Fact]
    public async Task DeniedLocation_UnresolvableSavedCity_FallsBackToSofia()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        weather.UnresolvedCities.Add("Атлантида");
        var js = new LocationJsFake
        {
            Result = new Weather.LocationResult(0, 0, "denied"),
            SavedCity = "Атлантида"
        };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();

            Assert.Equal(["Атлантида", "Sofia"], weather.RequestedCities);
            Assert.Null(state.CurrentLocation);
            Assert.Equal("Sofia", state.City);
            Assert.Equal("Sofia", state.LastSummary!.Name);
            Assert.Equal("Sofia", renderer.CityValue(page));
            Assert.Equal(3, renderer.TabCount(page));
            Assert.True(state.LocationAttempted);
            Assert.DoesNotContain("Градът не е намерен", renderer.Text(page));
        });
    }

    [Fact]
    public async Task OlderFailedCityRequest_DoesNotReplaceDeviceForecast()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var pendingSofia = new TaskCompletionSource<WeatherForecast?>();
        weather.NextForecast = pendingSofia.Task;
        var js = new LocationJsFake { Result = new Weather.LocationResult(0, 0, "denied") };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var (page, rendering) = renderer.BeginOpen();
            await WaitForAsync(() => weather.RequestedDays.Count == 1);

            // Device location wins while the older Sofia request is still running.
            js.Result = new Weather.LocationResult(45.123456, 12.345678, null, "София");
            await renderer.UseLocationAsync(page);

            Assert.Equal("София", state.LastSummary!.Name);
            Assert.Equal((45.123456, 12.345678), weather.RequestedCoordinates[^1]);

            pendingSofia.SetResult(null);
            await rendering;

            // Trigger a render so a stale error would surface over the valid device forecast.
            await renderer.SelectFirstDayTabAsync(page);

            Assert.Equal("София", state.City);
            Assert.Equal("София", state.LastSummary!.Name);
            Assert.Equal(3, renderer.TabCount(page));
            var text = renderer.Text(page);
            Assert.DoesNotContain("Градът не е намерен", text);
            Assert.DoesNotContain("Няма данни", text);
            renderer.Close(page);
        });
    }

    [Fact]
    public async Task SlowSearch_DoesNotOverrideNewerSuccessfulSearch()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake();
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            var pendingPlovdiv = new TaskCompletionSource<WeatherForecast?>();
            weather.NextForecast = pendingPlovdiv.Task;
            var plovdivSearch = await renderer.BeginSearchCityAsync(page, "Plovdiv");
            await WaitForAsync(() => weather.RequestedCities.Count == 1);

            await renderer.SearchCityAsync(page, "Varna");

            Assert.Equal("Varna", state.City);
            Assert.Equal(["Varna"], js.SavedCities);

            pendingPlovdiv.SetResult(WeatherServiceFake.CreateForecast(3));
            await plovdivSearch;

            Assert.Equal(["Plovdiv", "Varna"], weather.RequestedCities);
            Assert.Equal("Varna", state.City);
            Assert.Equal("Varna", state.LastSummary!.Name);
            Assert.Equal("Varna", renderer.CityValue(page));
            Assert.Equal(["Varna"], js.SavedCities);
            Assert.Equal(3, renderer.TabCount(page));
        });
    }

    [Fact]
    public async Task SuccessfulManualSearch_DuringStartupLookup_IsKeptAcrossNavigation()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var pending = new TaskCompletionSource<Weather.LocationResult>();
        var js = new LocationJsFake { PendingResult = pending.Task };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var (page, _) = renderer.BeginOpen();
            await WaitForAsync(() => js.Calls == 1);

            await renderer.SearchCityAsync(page, "Plovdiv");

            Assert.True(state.LocationAttempted);
            Assert.Equal("Plovdiv", state.City);
            Assert.Equal(["Plovdiv"], js.SavedCities);

            // Reopening must keep the manual choice and not repeat the device lookup.
            renderer.Close(page);
            // A buggy re-probe would resolve immediately instead of waiting, so the test fails fast.
            js.PendingResult = null;
            var returnedPage = await renderer.OpenAsync();

            Assert.Equal("Plovdiv", state.City);
            Assert.Equal("Plovdiv", renderer.CityValue(returnedPage));
            Assert.Equal("Plovdiv", state.LastSummary!.Name);
            Assert.Equal(3, renderer.TabCount(returnedPage));
            Assert.Equal(1, js.Calls);
            Assert.Equal(["Plovdiv"], weather.RequestedCities);
            Assert.Equal([(42.7, 23.3)], weather.RequestedCoordinates);

            pending.SetResult(new Weather.LocationResult(45.123456, 12.345678, null, "София"));
            await Task.Delay(100);

            Assert.Equal("Plovdiv", state.City);
            Assert.Equal("Plovdiv", state.LastSummary!.Name);
            Assert.DoesNotContain((45.123456, 12.345678), weather.RequestedCoordinates);
            renderer.Close(returnedPage);
        });
    }

    [Fact]
    public async Task LatestManualSearch_WinsWhenBothArePending()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var js = new LocationJsFake();
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var page = await renderer.OpenAsync();
            var pendingPlovdiv = new TaskCompletionSource<WeatherForecast?>();
            var pendingVarna = new TaskCompletionSource<WeatherForecast?>();

            weather.NextForecast = pendingPlovdiv.Task;
            var plovdivSearch = await renderer.BeginSearchCityAsync(page, "Plovdiv");
            await WaitForAsync(() => weather.RequestedCities.Count == 1);

            weather.NextForecast = pendingVarna.Task;
            var varnaSearch = await renderer.BeginSearchCityAsync(page, "Varna");
            await WaitForAsync(() => weather.RequestedCities.Count == 2);

            // The older search completes first and must not commit or save anything.
            pendingPlovdiv.SetResult(WeatherServiceFake.CreateForecast(3));
            await plovdivSearch;

            Assert.Equal("София", state.City);
            Assert.Empty(js.SavedCities);

            pendingVarna.SetResult(WeatherServiceFake.CreateForecast(3));
            await varnaSearch;

            Assert.Equal(["Plovdiv", "Varna"], weather.RequestedCities);
            Assert.Equal("Varna", state.City);
            Assert.Equal("Varna", state.LastSummary!.Name);
            Assert.Equal("Varna", renderer.CityValue(page));
            Assert.Equal(["Varna"], js.SavedCities);
            Assert.Equal(3, renderer.TabCount(page));
        });
    }

    [Fact]
    public async Task SearchForDetectedCity_WinsOverOlderPendingSearch()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var pendingDevice = new TaskCompletionSource<Weather.LocationResult>();
        var js = new LocationJsFake { PendingResult = pendingDevice.Task };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var (page, _) = renderer.BeginOpen();
            await WaitForAsync(() => js.Calls == 1);

            var pendingPlovdiv = new TaskCompletionSource<WeatherForecast?>();
            weather.NextForecast = pendingPlovdiv.Task;
            var plovdivSearch = await renderer.BeginSearchCityAsync(page, "Plovdiv");
            await WaitForAsync(() => weather.RequestedCities.Count == 1);

            // The device forecast for Sofia loads while the Plovdiv search is still pending.
            pendingDevice.SetResult(new Weather.LocationResult(45.123456, 12.345678, null, "София"));
            await WaitForAsync(() => state.CurrentLocation is not null && state.LastSummary?.Name == "София");

            await renderer.SearchCityAsync(page, "София");

            Assert.Equal(new WeatherState.Coordinates(45.123456, 12.345678), state.CurrentLocation);
            Assert.Equal(["София"], js.SavedCities);

            // The older Plovdiv search finishes last and must not replace the newer Sofia search.
            pendingPlovdiv.SetResult(WeatherServiceFake.CreateForecast(3));
            await plovdivSearch;

            Assert.Equal("София", state.City);
            Assert.Equal(new WeatherState.Coordinates(45.123456, 12.345678), state.CurrentLocation);
            Assert.Equal("София", state.LastSummary!.Name);
            Assert.Equal("София", renderer.CityValue(page));
            Assert.Equal(["Plovdiv"], weather.RequestedCities);
            Assert.Equal((45.123456, 12.345678), weather.RequestedCoordinates[^1]);
            Assert.Equal(["София"], js.SavedCities);
            Assert.Equal(3, renderer.TabCount(page));
            renderer.Close(page);
        });
    }

    [Fact]
    public async Task ManualSearch_Pending_KeepsDaySelectorDisabled()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var pendingDevice = new TaskCompletionSource<Weather.LocationResult>();
        var js = new LocationJsFake { PendingResult = pendingDevice.Task };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var (page, rendering) = renderer.BeginOpen();
            await WaitForAsync(() => js.Calls == 1);

            var pendingPlovdiv = new TaskCompletionSource<WeatherForecast?>();
            weather.NextForecast = pendingPlovdiv.Task;
            var plovdivSearch = await renderer.BeginSearchCityAsync(page, "Plovdiv");
            await WaitForAsync(() => weather.RequestedCities.Count == 1);

            // The automatic forecast finishes first and must not unlock the day selector.
            pendingDevice.SetResult(new Weather.LocationResult(45.123456, 12.345678, null, "София"));
            await rendering;

            Assert.Equal("София", state.LastSummary!.Name);
            Assert.True(renderer.DaySelectorDisabled(page));

            pendingPlovdiv.SetResult(WeatherServiceFake.CreateForecast(3));
            await plovdivSearch;

            Assert.Equal("Plovdiv", state.City);
            Assert.False(renderer.DaySelectorDisabled(page));

            await renderer.ChangeDaysAsync(page, 7);

            Assert.Equal(7, state.DisplayDays);
            Assert.Equal(7, renderer.TabCount(page));
            Assert.Equal("Plovdiv", state.LastSummary!.Name);
            Assert.Equal("Plovdiv", weather.RequestedCities[^1]);
            renderer.Close(page);
        });
    }

    [Fact]
    public async Task ManualSearch_Success_ClearsAutomaticForecastError()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var pendingDevice = new TaskCompletionSource<Weather.LocationResult>();
        var js = new LocationJsFake { PendingResult = pendingDevice.Task };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var (page, rendering) = renderer.BeginOpen();
            await WaitForAsync(() => js.Calls == 1);

            var pendingPlovdiv = new TaskCompletionSource<WeatherForecast?>();
            weather.NextForecast = pendingPlovdiv.Task;
            var plovdivSearch = await renderer.BeginSearchCityAsync(page, "Plovdiv");
            await WaitForAsync(() => weather.RequestedCities.Count == 1);

            // The automatic forecast fails while the manual search is still pending.
            weather.FailNextForecast = true;
            pendingDevice.SetResult(new Weather.LocationResult(45.123456, 12.345678, null, "София"));
            await rendering;

            Assert.Contains("Няма данни за метеорологичната прогноза.", renderer.Text(page));

            pendingPlovdiv.SetResult(WeatherServiceFake.CreateForecast(3));
            await plovdivSearch;

            var text = renderer.Text(page);
            Assert.Equal("Plovdiv", state.City);
            Assert.Equal("Plovdiv", state.LastSummary!.Name);
            Assert.Contains("Plovdiv", text);
            Assert.DoesNotContain("Няма данни за метеорологичната прогноза.", text);
            Assert.Equal(3, renderer.TabCount(page));
            renderer.Close(page);
        });
    }

    [Fact]
    public async Task FailedManualSearch_Error_SurvivesSuccessfulAutomaticForecast()
    {
        var state = new WeatherState();
        var weather = new WeatherServiceFake();
        var pendingDevice = new TaskCompletionSource<Weather.LocationResult>();
        var js = new LocationJsFake { PendingResult = pendingDevice.Task };
        await using var services = CreateServices(state, weather, js);
        await using var renderer = new WeatherRenderer(services);

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var (page, rendering) = renderer.BeginOpen();
            await WaitForAsync(() => js.Calls == 1);

            var pendingMisspelled = new TaskCompletionSource<(double lat, double lon, string? name)?>();
            weather.NextGeocode = pendingMisspelled.Task;
            var misspelledSearch = await renderer.BeginSearchCityAsync(page, "Plovdvi");
            await WaitForAsync(() => weather.RequestedCities.Count == 1);

            // Device location returns Sofia while Sofia's forecast request is held open.
            var pendingSofiaForecast = new TaskCompletionSource<WeatherForecast?>();
            weather.NextForecast = pendingSofiaForecast.Task;
            pendingDevice.SetResult(new Weather.LocationResult(45.123456, 12.345678, null, "София"));
            await WaitForAsync(() => weather.RequestedDays.Count == 1);

            pendingMisspelled.SetResult(null);
            await misspelledSearch;

            Assert.Contains("Градът не е намерен.", renderer.Text(page));

            pendingSofiaForecast.SetResult(WeatherServiceFake.CreateForecast(3));
            await rendering;

            Assert.Equal("София", state.City);
            Assert.Equal("София", state.LastSummary!.Name);
            Assert.Contains("София", renderer.Text(page));
            Assert.Equal(3, renderer.TabCount(page));
            Assert.Contains("Градът не е намерен.", renderer.Text(page));
            renderer.Close(page);
        });
    }

    static async Task WaitForAsync(Func<bool> condition)
    {
        var ct = TestContext.Current.CancellationToken;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    static ServiceProvider CreateServices(WeatherState state, WeatherServiceFake weather, LocationJsFake? js = null)
        => new ServiceCollection()
            .AddLogging()
            .AddSingleton(state)
            .AddSingleton<IWeatherService>(weather)
            .AddSingleton<IJSRuntime>(js ?? new LocationJsFake())
            .BuildServiceProvider();

    static WeatherState.Summary CreateStoredSummary(string name)
        => new(
            name,
            42.7,
            23.3,
            Enumerable.Range(0, 3)
                .Select(day => new WeatherState.DaySummary(
                    new DateOnly(2026, 9, 6).AddDays(day),
                    Enumerable.Range(0, 24)
                        .Select(hour => new WeatherState.HourEntry(
                            new DateTime(2026, 9, 6).AddDays(day).AddHours(hour),
                            20,
                            5,
                            10,
                            0))
                        .ToList()))
                .ToList());

    sealed class LocationJsFake : IJSRuntime
    {
        public Weather.LocationResult Result { get; set; } = new(45.123456, 12.345678, null, "София");
        public Task<Weather.LocationResult>? PendingResult { get; set; }
        public string? SavedCity { get; set; }
        public int Calls { get; private set; }
        public int SavedCityReads { get; private set; }
        public int HourScrollCalls { get; private set; }
        public List<string> SavedCities { get; } = [];
        public List<string?> HourKeys { get; } = [];
        public List<string?> HourTimeZones { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "weatherForecast.showCurrentHour")
            {
                HourScrollCalls++;
                HourKeys.Add(args?[1]?.ToString());
                HourTimeZones.Add(args?[2]?.ToString());
                return default!;
            }

            if (identifier == "weatherLocation.getSavedCity")
            {
                SavedCityReads++;
                return (TValue)(object?)SavedCity!;
            }

            if (identifier == "weatherLocation.saveCity")
            {
                SavedCities.Add(args![0]!.ToString()!);
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
        public HashSet<string> UnresolvedCities { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool FailNextForecast { get; set; }
        public bool FailNextGeocode { get; set; }
        public Task<(double lat, double lon, string? name)?>? NextGeocode { get; set; }
        public Task<WeatherForecast?>? NextForecast { get; set; }

        public Task<(double lat, double lon, string? name)?> FetchGeocode(string city, string? language = "bg")
        {
            RequestedCities.Add(city);
            if (NextGeocode is { } pending)
            {
                NextGeocode = null;
                return pending;
            }

            if (FailNextGeocode)
            {
                FailNextGeocode = false;
                return Task.FromResult<(double, double, string?)?>(null);
            }

            if (UnresolvedCities.Contains(city))
            {
                return Task.FromResult<(double, double, string?)?>(null);
            }

            return Task.FromResult<(double, double, string?)?>((42.7, 23.3, city));
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
            return new WeatherForecast(
                hours,
                new CurrentWeather(start.AddHours(14), 20, 0),
                "Europe/Sofia");
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
            var (id, rendering) = BeginOpen();
            await rendering;
            return id;
        }

        // Lets a test drive the page while the first render is still waiting for a pending JS call.
        public (int Id, Task Rendering) BeginOpen()
        {
            var id = AssignRootComponentId(InstantiateComponent(typeof(Weather)));
            var rendering = RenderRootComponentAsync(id);
            return (id, rendering);
        }

        public void Close(int id) => RemoveRootComponent(id);

        public Task UseLocationAsync(int id)
        {
            var handler = ElementAttributes(id, "button", "location-button").Single(frame => frame.AttributeName == "onclick");
            return DispatchEventAsync(handler.AttributeEventHandlerId, null, new MouseEventArgs());
        }

        // Re-renders the page without changing weather data, so pending state becomes visible.
        public Task SelectFirstDayTabAsync(int id)
        {
            var frames = Frames(id).ToArray();
            for (var i = 0; i < frames.Length; i++)
            {
                if (frames[i].FrameType != RenderTreeFrameType.Element || frames[i].ElementName != "button")
                {
                    continue;
                }

                var attributes = frames.Skip(i + 1).TakeWhile(frame => frame.FrameType == RenderTreeFrameType.Attribute).ToArray();
                if (attributes.Any(frame => frame.AttributeName == "role" && Equals(frame.AttributeValue, "tab")))
                {
                    var handler = attributes.Single(frame => frame.AttributeName == "onclick");
                    return DispatchEventAsync(handler.AttributeEventHandlerId, null, new MouseEventArgs());
                }
            }

            throw new InvalidOperationException("Day tab not found");
        }

        public async Task SearchCityAsync(int id, string city)
        {
            var input = ElementAttributes(id, "input").Single(frame => frame.AttributeName == "onchange");
            await DispatchEventAsync(input.AttributeEventHandlerId, null, new ChangeEventArgs { Value = city });
            var form = ElementAttributes(id, "form").Single(frame => frame.AttributeName == "onsubmit");
            await DispatchEventAsync(form.AttributeEventHandlerId, null, EventArgs.Empty);
        }

        // Starts a city search and returns its pending event task without waiting for it.
        public async Task<Task> BeginSearchCityAsync(int id, string city)
        {
            var input = ElementAttributes(id, "input").Single(frame => frame.AttributeName == "onchange");
            await DispatchEventAsync(input.AttributeEventHandlerId, null, new ChangeEventArgs { Value = city });
            var form = ElementAttributes(id, "form").Single(frame => frame.AttributeName == "onsubmit");
            return DispatchEventAsync(form.AttributeEventHandlerId, null, EventArgs.Empty);
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

        public bool DaySelectorDisabled(int id)
            => SelectAttributes(id).Any(frame => frame.AttributeName == "disabled" && frame.AttributeValue is true);

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
