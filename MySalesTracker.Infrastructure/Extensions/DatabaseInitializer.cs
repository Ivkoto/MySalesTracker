using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySalesTracker.Infrastructure.Persistence;

namespace MySalesTracker.Infrastructure.Extensions;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var context = await dbFactory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
        await DataSeeder.RunAsync(context);
    }
}
