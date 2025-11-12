using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
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

        endpoints.MapHub<SalesHub>(hubPath, options =>
        {
            options.Transports = HttpTransportType.WebSockets |
                                 HttpTransportType.ServerSentEvents |
                                 HttpTransportType.LongPolling;
        });

        return endpoints;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Logout endpoint - separate from Blazor components to avoid "headers already sent" issue
        endpoints.MapGet("/logout", async context =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect("/login");
            
        }).AllowAnonymous();

        return endpoints;
    }
}
