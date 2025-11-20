using Microsoft.Extensions.DependencyInjection;
using MySalesTracker.Application.Services;

namespace MySalesTracker.Application.Extensions;
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<EventService>();
        services.AddScoped<SaleService>();
        services.AddScoped<ProductService>();
        services.AddScoped<PriceRuleService>();
        services.AddScoped<PaymentService>();

        return services;
    }
}
