using System.Globalization;
using System.Net;
using MySalesTracker.Infrastructure.ExternalServices;

namespace MySalesTracker.Tests;

public sealed class WeatherServiceTests
{
    [Fact]
    public async Task Forecast_FormatsCoordinatesAndMapsCurrentConditions()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("bg-BG");
            var handler = new ForecastHandler();
            using var http = new HttpClient(handler);

            var forecast = await new WeatherService(http).GetForecast(45.123456, 12.345678, 5);

            Assert.Contains("latitude=45.123456&longitude=12.345678", handler.RequestUri!.Query);
            Assert.Contains("forecast_days=5", handler.RequestUri.Query);
            Assert.Contains("current=temperature_2m,rain", handler.RequestUri.Query);
            Assert.Equal(new DateTime(2026, 9, 11, 14, 15, 0), forecast!.Current!.Time);
            Assert.Equal(24.2, forecast.Current.Temperature);
            Assert.Equal(0.4, forecast.Current.Rainfall);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    sealed class ForecastHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"current\":{\"time\":\"2026-09-11T14:15\",\"temperature_2m\":24.2,\"rain\":0.4},\"hourly\":{\"time\":[]}}")
            });
        }
    }
}
