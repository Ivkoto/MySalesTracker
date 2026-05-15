using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Infrastructure.ExternalServices;
using MySalesTracker.Infrastructure.Persistence;
using MySalesTracker.Infrastructure.Persistence.Repositories;
using MySalesTracker.Infrastructure.Services;

namespace MySalesTracker.Infrastructure.Extensions;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var aspireConn = configuration.GetConnectionString("sql2017");
        var defaultConn = configuration.GetConnectionString("DatabaseConnection");
        var connString = !string.IsNullOrEmpty(aspireConn) ? aspireConn : defaultConn;

        if (string.IsNullOrWhiteSpace(connString))
        {
            throw new InvalidOperationException("No valid database connection string configured. Configure either 'sql2017' or 'DatabaseConnection' in the connectionStrings section.");
        }

        services.AddDbContextFactory<AppDbContext>(opt => opt.UseSqlServer(connString));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPriceRuleRepository, PriceRuleRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        services.AddHttpClient<IWeatherService, WeatherService>();

        services.AddScoped<INotificationService, SignalRNotificationService>();

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = environment.IsDevelopment();

            //Increase timeouts for mobile/slow connections
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.HandshakeTimeout = TimeSpan.FromSeconds(30);
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            //Maximum message size (default is 32KB, increase for larger payloads if needed)
            options.MaximumReceiveMessageSize = 102400; // 100KB
            //TODO: Remove or increase if this works worst.
            options.MaximumParallelInvocationsPerClient = 1;
        });

        return services;
    }
}
