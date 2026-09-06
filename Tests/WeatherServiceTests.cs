using System.Globalization;
using System.Net;
using MySalesTracker.Infrastructure.ExternalServices;

namespace MySalesTracker.Tests;

public sealed class WeatherServiceTests
{
    [Fact]
    public async Task Forecast_FormatsCoordinatesForApiUnderBulgarianCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("bg-BG");
            var handler = new ForecastHandler();
            using var http = new HttpClient(handler);

            await new WeatherService(http).GetForecast(45.123456, 12.345678, 5);

            Assert.Contains("latitude=45.123456&longitude=12.345678", handler.RequestUri!.Query);
            Assert.Contains("forecast_days=5", handler.RequestUri.Query);
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
                Content = new StringContent("{\"hourly\":{\"time\":[]}}")
            });
        }
    }
}
