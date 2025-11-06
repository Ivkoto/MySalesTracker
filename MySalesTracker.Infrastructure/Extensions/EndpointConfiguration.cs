using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using MySalesTracker.Infrastructure.Hubs;

namespace MySalesTracker.Infrastructure.Extensions;
public static class EndpointConfiguration
{
    public static IEndpointRouteBuilder MapInfrastructureEndpoints(this IEndpointRouteBuilder endpoints, IConfiguration configuration)
    {
        var hubPath = configuration["SignalR:SalesHubPath"];

        if (string.IsNullOrWhiteSpace(hubPath))
        {
            throw new InvalidOperationException("SignalR:SalesHubPath must be configured in appsettings.json");
        }

        endpoints.MapHub<SalesHub>(hubPath);

        return endpoints;
    }
}
