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
    public static async Task RunAsync(AppDbContext db)
    {
        if (await db.Products.AnyAsync()) return;

        var pBandana = new Product { Name = "Бандани", Brand = Brand.Totem };
        var pGlove = new Product { Name = "Ръкавици", Brand = Brand.Totem };
        var pCandle = new Product { Name = "Свещи", Brand = Brand.Candles };
        var pBags = new Product { Name = "Торби", Brand = Brand.Candles }; // only for Candles
        var pCeramic = new Product { Name = "Керамика", Brand = Brand.Ceramics };

        db.AddRange(pBandana, pGlove, pCandle, pBags, pCeramic);

        // Candles: 19→1, 29→1, 38→2, 57→3
        db.PriceRules.AddRange(
            PR(pCandle, 19, 1, 1), PR(pCandle, 29, 1, 2),
            PR(pCandle, 38, 2, 3), PR(pCandle, 57, 3, 4)
        );

        // Bags (Candles brand): 2.00→1, 4.00→2, 6.00→3, 8.00→4
        db.PriceRules.AddRange(
            PR(pBags, 2.00m, 1, 1), PR(pBags, 4.00m, 2, 2),
            PR(pBags, 6.00m, 3, 3), PR(pBags, 8.00m, 4, 4)
        );

        // Gloves: 30/35/40→1, 70/80→2, 105/120→3
        db.PriceRules.AddRange(
            PR(pGlove, 30, 1, 1), PR(pGlove, 35, 1, 2), PR(pGlove, 40, 1, 3),
            PR(pGlove, 70, 2, 4), PR(pGlove, 80, 2, 5),
            PR(pGlove, 105, 3, 6), PR(pGlove, 120, 3, 7)
        );

        // Bandanas – placeholder examples
        db.PriceRules.AddRange(
            PR(pBandana, 30, 1, 1), PR(pBandana, 35, 1, 2), PR(pBandana, 40, 1, 3),
            PR(pBandana, 70, 2, 4), PR(pBandana, 80, 2, 5),
            PR(pBandana, 105, 3, 6), PR(pBandana, 120, 3, 7)
        );

        await db.SaveChangesAsync();

        static PriceRule PR(Product p, decimal price, int units, int sort)
            => new() { Product = p, Price = price, UnitsPerSale = units, SortOrder = sort };
    }
}
