using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventDay> EventDays => Set<EventDay>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<Sale> Sale => Set<Sale>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<PriceRule>().HasIndex(r => new { r.ProductId, r.Price });
        b.Entity<Sale>().HasIndex(s => s.EventDayId);

        // That converter is needed to store Product.Brand as Bulgarian strings in the database
        // while keeping Brand as an enum in code.
        var brandToString = new ValueConverter<Brand, string>(
            v => v == Brand.Totem ? "ТОТЕМ"
               : v == Brand.Ceramics ? "Керамика"
               : v == Brand.Candles ? "Гора"
               : v.ToString(),
            v => v == "ТОТЕМ" ? Brand.Totem
               : v == "Керамика" ? Brand.Ceramics
               : v == "Гора" ? Brand.Candles
               : Brand.Totem
        );

        b.Entity<Product>()
            .Property(p => p.Brand)
            .HasConversion(brandToString)
            .HasMaxLength(50);

        b.Entity<Sale>()
            .Property(s => s.Currency)
            .HasDefaultValue(Currency.EUR);
        
        b.Entity<PriceRule>()
            .Property(pr => pr.Currency)
            .HasDefaultValue(Currency.EUR);
        
        b.Entity<Payment>()
            .Property(p => p.Currency)
            .HasDefaultValue(Currency.EUR);

        b.Entity<EventDay>()
            .Property(e => e.StartingPettyCashCurrency)
            .HasDefaultValue(Currency.EUR);
        
        b.Entity<Expense>()
            .Property(e => e.Currency)
            .HasDefaultValue(Currency.EUR);
    }

}
