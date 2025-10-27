using Microsoft.EntityFrameworkCore;
using MySalesTracker.Data.Models;

namespace MySalesTracker.Data;

/// <summary>
/// Seeds the initial dataset we already maintain in Google Spreadsheets.
/// It mirrors today’s known structure and values (brands, products, and price rules)
/// so the application starts with familiar data and can replace the spreadsheet workflow.
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

        var effectiveFrom = new DateOnly(2020, 1, 1);

        // Candles (Gora brand): 19→1, 29→1, 38→2, 57→3
        context.PriceRules.AddRange(
            PR(pCandle, 19.00m, 1, 1), PR(pCandle, 29.00m, 1, 2),
            PR(pCandle, 38.00m, 2, 3), PR(pCandle, 57.00m, 3, 4)
        );

        // Bags (Gora brand): 2.00→1, 4.00→2, 6.00→3, 8.00→4
        context.PriceRules.AddRange(
            PR(pBags, 2.00m, 1, 1), PR(pBags, 4.00m, 2, 2),
            PR(pBags, 6.00m, 3, 3), PR(pBags, 8.00m, 4, 4)
        );

        // Matches (Gora brand): 7→1, 14→2, 21→3, 28→4
        context.PriceRules.AddRange(
            PR(pMatches, 7.00m, 1, 1), PR(pMatches, 14.00m, 2, 2),
            PR(pMatches, 21.00m, 3, 3), PR(pMatches, 28.00m, 4, 4)
        );

        // Gloves (Totem brand): 30/35/40→1, 70/80→2, 105/120→3
        context.PriceRules.AddRange(
            PR(pGlove, 30.00m, 1, 1), PR(pGlove, 35.00m, 1, 2),
            PR(pGlove, 40.00m, 1, 3), PR(pGlove, 70.00m, 2, 4),
            PR(pGlove, 80.00m, 2, 5), PR(pGlove, 105.00m, 3, 6),
            PR(pGlove, 120.00m, 3, 7)
        );

        // Bandanas (Totem brand): 30/35/40→1, 70/80→2, 105/120→3
        context.PriceRules.AddRange(
            PR(pBandana, 30.00m, 1, 1), PR(pBandana, 35.00m, 1, 2), PR(pBandana, 40.00m, 1, 3),
            PR(pBandana, 70.00m, 2, 4), PR(pBandana, 80.00m, 2, 5),
            PR(pBandana, 105.00m, 3, 6), PR(pBandana, 120.00m, 3, 7)
        );

        await context.SaveChangesAsync();

        static PriceRule PR(Product p, decimal price, int units, int sort)
            => new() { Product = p, Price = price, UnitsPerSale = units, SortOrder = sort, EffectiveFrom = new DateOnly(2020, 1, 1)};
    }
}
