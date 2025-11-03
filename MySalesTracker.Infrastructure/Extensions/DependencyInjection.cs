using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Infrastructure.ExternalServices;
using MySalesTracker.Infrastructure.Persistence;
using MySalesTracker.Infrastructure.Persistence.Repositories;
using MySalesTracker.Infrastructure.Services;

namespace MySalesTracker.Infrastructure.Extensions;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextFactory<AppDbContext>(opt => opt.UseSqlServer(configuration.GetConnectionString("DatabaseConnection")));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPriceRuleRepository, PriceRuleRepository>();

        services.AddHttpClient<IWeatherService, WeatherService>();

        services.AddScoped<INotificationService, SignalRNotificationService>();
        services.AddSignalR();

        return services;
    }
}
