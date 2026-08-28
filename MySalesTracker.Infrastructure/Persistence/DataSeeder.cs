using Microsoft.EntityFrameworkCore;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Infrastructure.Persistence;

/// <summary>
/// Seeds the product catalog for an empty database.
/// </summary>
public static class DataSeeder
{
    public static async Task RunAsync(AppDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

        var pBandana = new Product { Name = "Бандани", Brand = Brand.Totem };
        var pGlove = new Product { Name = "Ръкавици", Brand = Brand.Totem };
        var pCandle = new Product { Name = "Свещ", Brand = Brand.Candles };
        var pMatches = new Product { Name = "Кибрит", Brand = Brand.Candles };
        var pBags = new Product { Name = "Торби", Brand = Brand.Candles };
        var pCeramic = new Product { Name = "Керамика", Brand = Brand.Ceramics };

        context.AddRange(pBandana, pGlove, pCandle, pMatches, pBags, pCeramic);
        await context.SaveChangesAsync();
    }
}
