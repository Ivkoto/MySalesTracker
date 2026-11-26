using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;
using MySalesTracker.Domain.Services;

namespace MySalesTracker.Tests;

public sealed class SalesCalculationsTests
{
    [Fact]
    public void GetProductQuantitiesByBrand_SumsUnitsAndKeepsSameNameSeparatePerBrand()
    {
        Sale[] sales =
        [
            CreateSale(Brand.Totem, "Комплект", 2),
            CreateSale(Brand.Totem, "Комплект", 3),
            CreateSale(Brand.Candles, "Комплект", 4)
        ];

        var quantities = SalesCalculations.GetProductQuantitiesByBrand(SalesCalculations.GroupSalesByBrand(sales));

        Assert.Equal(5, quantities[Brand.Totem]["Комплект"]);
        Assert.Equal(4, quantities[Brand.Candles]["Комплект"]);
    }

    private static Sale CreateSale(Brand brand, string productName, int quantityUnits)
        => new()
        {
            Product = new Product { Name = productName, Brand = brand },
            QuantityUnits = quantityUnits
        };
}
