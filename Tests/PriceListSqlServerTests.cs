using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Application.Services;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Tests;

/// <summary>
/// Checks that need the real SQL Server provider: schema upgrade of existing data,
/// list-scoped replacement, and the unique indexes behind the price lists.
/// </summary>
public sealed class PriceListSqlServerTests
{
    const string PreviousMigration = "20261122131908_AddUniqueActiveProductName";

    [Fact]
    public async Task ExistingEventsAndPriceRules_BecomeBG_AndKeepPricesAndSoldUnits()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await CreateOrSkipAsync(ct);

        await using (var context = database.CreateContext())
        {
            await context.Database.GetService<IMigrator>().MigrateAsync(PreviousMigration, ct);
        }

        await database.ExecuteAsync(
            """
            INSERT INTO Events (Name, StartDate, EndDate) VALUES (N'Legacy', '2026-11-05', '2026-11-07');
            INSERT INTO EventDays (EventId, Date, StartingPettyCash, StartingPettyCashCurrency) VALUES (1, '2026-11-05', 10.00, 0);
            INSERT INTO Products (Name, Brand, IsActive) VALUES (N'Свещ', N'Гора', 1);
            INSERT INTO PriceRules (Price, UnitsPerSale, ProductId, SortOrder, Currency, IsDefault) VALUES (20.00, 2, 1, 1, 0, 1);
            INSERT INTO Sale (EventDayId, ProductId, Price, QuantityUnits, DiscountValue, Notes, CreatedUtc, Currency)
                VALUES (1, 1, 20.00, 2, 0.00, NULL, '2026-11-05T10:00:00', 0);
            """,
            ct);

        await using (var context = database.CreateContext())
        {
            await context.Database.MigrateAsync(ct);
        }

        Assert.Equal(0, await database.ScalarAsync("SELECT PriceList FROM Events WHERE Name = N'Legacy'", ct));
        Assert.Equal(0, await database.ScalarAsync("SELECT COUNT(*) FROM Events WHERE PriceList = 1", ct));
        Assert.Equal(0, await database.ScalarAsync("SELECT COUNT(*) FROM PriceRules WHERE PriceList = 1", ct));
        Assert.Equal(1, await database.ScalarAsync("SELECT COUNT(*) FROM Events", ct));

        var priceRule = await database.ScalarAsync(
            "SELECT CONCAT(Price, '|', UnitsPerSale, '|', PriceList, '|', IsDefault) FROM PriceRules", ct);
        Assert.Equal("20.00|2|0|1", priceRule);

        var sale = await database.ScalarAsync("SELECT CONCAT(Price, '|', QuantityUnits) FROM Sale", ct);
        Assert.Equal("20.00|2", sale);
    }

    [Fact]
    public async Task ReplacingOneList_PreservesTheOther_AndIndexesEnforceBothRules()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await CreateOrSkipAsync(ct);

        await using (var context = database.CreateContext())
        {
            await context.Database.MigrateAsync(ct);
        }

        await database.ExecuteAsync(
            "INSERT INTO Products (Name, Brand, IsActive) VALUES (N'Свещ', N'Гора', 1);",
            ct);

        await using var provider = TestInfrastructureServices.Create(database.ConnectionString);
        var repository = provider.GetRequiredService<IPriceRuleRepository>();

        await repository.ReplacePriceRulesAsync(1, PriceList.BG,
        [
            CreateRule(PriceList.BG, 20m, unitsPerSale: 2, isDefault: true),
            CreateRule(PriceList.BG, 30m, unitsPerSale: 1, isDefault: false)
        ], ct);
        await repository.ReplacePriceRulesAsync(1, PriceList.EU,
        [
            CreateRule(PriceList.EU, 20m, unitsPerSale: 1, isDefault: true),
            CreateRule(PriceList.EU, 40m, unitsPerSale: 3, isDefault: false)
        ], ct);

        await repository.ReplacePriceRulesAsync(1, PriceList.EU,
        [
            CreateRule(PriceList.EU, 25m, unitsPerSale: 3, isDefault: true)
        ], ct);

        var bg = await repository.GetAllPriceRulesAsync(PriceList.BG, ct);
        Assert.Collection(
            bg.OrderBy(rule => rule.SortOrder),
            first =>
            {
                Assert.Equal(20m, first.Price);
                Assert.Equal(2, first.UnitsPerSale);
                Assert.True(first.IsDefault);
            },
            second =>
            {
                Assert.Equal(30m, second.Price);
                Assert.Equal(1, second.UnitsPerSale);
                Assert.False(second.IsDefault);
            });

        var eu = Assert.Single(await repository.GetAllPriceRulesAsync(PriceList.EU, ct));
        Assert.Equal(25m, eu.Price);
        Assert.Equal(3, eu.UnitsPerSale);
        Assert.True(eu.IsDefault);

        // Same price in both lists is valid.
        await database.ExecuteAsync(
            "INSERT INTO PriceRules (Price, UnitsPerSale, ProductId, SortOrder, Currency, IsDefault, PriceList) VALUES (20.00, 1, 1, 2, 0, 0, 1);",
            ct);
        Assert.Equal(1, await database.ScalarAsync("SELECT COUNT(*) FROM PriceRules WHERE PriceList = 1 AND Price = 20.00", ct));

        // One default per list stays valid.
        Assert.Equal(1, await database.ScalarAsync("SELECT COUNT(*) FROM PriceRules WHERE PriceList = 0 AND IsDefault = 1", ct));
        Assert.Equal(1, await database.ScalarAsync("SELECT COUNT(*) FROM PriceRules WHERE PriceList = 1 AND IsDefault = 1", ct));

        // Duplicate price inside one list is rejected.
        await AssertIndexViolationAsync(database,
            "INSERT INTO PriceRules (Price, UnitsPerSale, ProductId, SortOrder, Currency, IsDefault, PriceList) VALUES (30.00, 1, 1, 3, 0, 0, 0);",
            ct);

        // A second default inside one list is rejected.
        await AssertIndexViolationAsync(database,
            "INSERT INTO PriceRules (Price, UnitsPerSale, ProductId, SortOrder, Currency, IsDefault, PriceList) VALUES (70.00, 1, 1, 4, 0, 1, 1);",
            ct);
    }

    [Fact]
    public async Task EventsCreatedThroughService_ResolveUnitsFromTheirOwnPersistedPriceList()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await CreateOrSkipAsync(ct);

        await using (var context = database.CreateContext())
        {
            await context.Database.MigrateAsync(ct);
        }

        await database.ExecuteAsync(
            """
            INSERT INTO Products (Name, Brand, IsActive) VALUES (N'Свещ', N'Гора', 1);
            INSERT INTO PriceRules (Price, UnitsPerSale, ProductId, SortOrder, Currency, IsDefault, PriceList) VALUES
                (20.00, 2, 1, 1, 0, 1, 0),
                (20.00, 1, 1, 1, 0, 1, 1),
                (30.00, 2, 1, 2, 0, 0, 1);
            """,
            ct);

        await using var provider = TestInfrastructureServices.Create(database.ConnectionString);
        var eventRepository = provider.GetRequiredService<IEventRepository>();
        var eventService = new EventService(eventRepository, NullLogger<EventService>.Instance);
        var priceService = new PriceRuleService(
            provider.GetRequiredService<IPriceRuleRepository>(),
            provider.GetRequiredService<IProductRepository>(),
            NullLogger<PriceRuleService>.Instance);

        var start = new DateOnly(2026, 11, 5);
        var bgCreated = await eventService.CreateEventAsync("BG базар", start, start, PriceList.BG, ct);
        var euCreated = await eventService.CreateEventAsync("EU базар", start, start, PriceList.EU, ct);
        Assert.True(bgCreated.Success);
        Assert.True(euCreated.Success);

        // Read the days back so the list comes from the persisted event, not from the request.
        var bgDay = await eventRepository.GetEventDayByIdAsync(bgCreated.Data!.Days.Single().EventDayId, ct);
        var euDay = await eventRepository.GetEventDayByIdAsync(euCreated.Data!.Days.Single().EventDayId, ct);

        Assert.Equal(PriceList.BG, bgDay!.Event.PriceList);
        Assert.Equal(PriceList.EU, euDay!.Event.PriceList);

        Assert.Equal(2, await priceService.GetUnitsForProductAsync(1, bgDay.Event.PriceList, 20m, ct));
        Assert.Equal(1, await priceService.GetUnitsForProductAsync(1, euDay.Event.PriceList, 20m, ct));
        Assert.Null(await priceService.GetUnitsForProductAsync(1, bgDay.Event.PriceList, 30m, ct));
        Assert.Equal(2, await priceService.GetUnitsForProductAsync(1, euDay.Event.PriceList, 30m, ct));
    }

    [Fact]
    public async Task Downgrade_WithOnlyBGData_SucceedsAndKeepsData()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await CreateOrSkipAsync(ct);
        await MigrateToCurrentAsync(database, ct);

        await database.ExecuteAsync(
            """
            INSERT INTO Events (Name, StartDate, EndDate, PriceList) VALUES (N'BG базар', '2026-11-05', '2026-11-07', 0);
            INSERT INTO EventDays (EventId, Date, StartingPettyCash, StartingPettyCashCurrency) VALUES (1, '2026-11-05', 10.00, 0);
            INSERT INTO Products (Name, Brand, IsActive) VALUES (N'Свещ', N'Гора', 1);
            INSERT INTO PriceRules (Price, UnitsPerSale, ProductId, SortOrder, Currency, IsDefault, PriceList) VALUES (20.00, 2, 1, 1, 0, 1, 0);
            INSERT INTO Sale (EventDayId, ProductId, Price, QuantityUnits, DiscountValue, Notes, CreatedUtc, Currency)
                VALUES (1, 1, 20.00, 2, 0.00, NULL, '2026-11-05T10:00:00', 0);
            """,
            ct);

        await using (var context = database.CreateContext())
        {
            await context.Database.GetService<IMigrator>().MigrateAsync(PreviousMigration, ct);
        }

        Assert.Equal(0, await database.ScalarAsync(
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('Events') AND name = 'PriceList'", ct));
        Assert.Equal(0, await database.ScalarAsync(
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('PriceRules') AND name = 'PriceList'", ct));
        Assert.Equal("BG базар", await database.ScalarAsync("SELECT Name FROM Events", ct));
        Assert.Equal("20.00|2", await database.ScalarAsync("SELECT CONCAT(Price, '|', UnitsPerSale) FROM PriceRules", ct));
        Assert.Equal("20.00|2", await database.ScalarAsync("SELECT CONCAT(Price, '|', QuantityUnits) FROM Sale", ct));
        Assert.Equal(1, await database.ScalarAsync(
            "SELECT COUNT(*) FROM sys.indexes WHERE name = 'IX_PriceRules_ProductId_Price'", ct));
    }

    [Fact]
    public async Task Downgrade_WithEUEvent_IsBlockedByGuard()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await CreateOrSkipAsync(ct);
        await MigrateToCurrentAsync(database, ct);

        await database.ExecuteAsync(
            """
            INSERT INTO Events (Name, StartDate, EndDate, PriceList) VALUES (N'EU базар', '2026-11-05', '2026-11-07', 1);
            INSERT INTO Products (Name, Brand, IsActive) VALUES (N'Свещ', N'Гора', 1);
            INSERT INTO PriceRules (Price, UnitsPerSale, ProductId, SortOrder, Currency, IsDefault, PriceList) VALUES (20.00, 2, 1, 1, 0, 1, 0);
            """,
            ct);

        await AssertDowngradeBlockedAsync(database, ct);

        Assert.Equal(1, await database.ScalarAsync("SELECT COUNT(*) FROM Events WHERE PriceList = 1", ct));
        Assert.Equal(1, await database.ScalarAsync(
            "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId LIKE '%_AddPriceLists'", ct));
    }

    [Fact]
    public async Task Downgrade_WithEUPriceRule_IsBlockedByGuard()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await CreateOrSkipAsync(ct);
        await MigrateToCurrentAsync(database, ct);

        await database.ExecuteAsync(
            """
            INSERT INTO Events (Name, StartDate, EndDate, PriceList) VALUES (N'BG базар', '2026-11-05', '2026-11-07', 0);
            INSERT INTO Products (Name, Brand, IsActive) VALUES (N'Свещ', N'Гора', 1);
            INSERT INTO PriceRules (Price, UnitsPerSale, ProductId, SortOrder, Currency, IsDefault, PriceList) VALUES (20.00, 2, 1, 1, 0, 1, 1);
            """,
            ct);

        await AssertDowngradeBlockedAsync(database, ct);

        Assert.Equal(1, await database.ScalarAsync("SELECT COUNT(*) FROM PriceRules WHERE PriceList = 1", ct));
        Assert.Equal(1, await database.ScalarAsync(
            "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId LIKE '%_AddPriceLists'", ct));
    }

    static async Task MigrateToCurrentAsync(SqlServerTestDatabase database, CancellationToken ct)
    {
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync(ct);
    }

    static async Task AssertDowngradeBlockedAsync(SqlServerTestDatabase database, CancellationToken ct)
    {
        var exception = await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await using var context = database.CreateContext();
            await context.Database.GetService<IMigrator>().MigrateAsync(PreviousMigration, ct);
        });

        // The guard must fail before any index or column is dropped.
        Assert.Equal(50010, exception.Number);
        Assert.Contains("would discard EU price-list data", exception.Message, StringComparison.Ordinal);
    }

    static async Task<SqlServerTestDatabase> CreateOrSkipAsync(CancellationToken ct)
    {
        var database = await SqlServerTestDatabase.TryCreateAsync(ct);
        Assert.SkipWhen(database is null, "SQL Server test instance is not reachable; set MYSALESTRACKER_TEST_SQL to enable these checks.");
        return database!;
    }

    static async Task AssertIndexViolationAsync(SqlServerTestDatabase database, string sql, CancellationToken ct)
    {
        var exception = await Assert.ThrowsAsync<SqlException>(() => database.ExecuteAsync(sql, ct));
        Assert.Contains(exception.Number, new[] { 2601, 2627 });
    }

    static Domain.Entities.PriceRule CreateRule(PriceList priceList, decimal price, int unitsPerSale, bool isDefault)
        => new()
        {
            ProductId = 1,
            PriceList = priceList,
            Price = price,
            Currency = Currency.EUR,
            UnitsPerSale = unitsPerSale,
            SortOrder = price == 20m ? 1 : 2,
            IsDefault = isDefault
        };
}
