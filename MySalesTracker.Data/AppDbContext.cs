using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MySalesTracker.Data.Models;

namespace MySalesTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventDay> EventDays => Set<EventDay>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<Sale> Sale => Set<Sale>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // DateOnly → date
        b.Entity<Event>().Property(x => x.StartDate).HasColumnType("date");
        b.Entity<Event>().Property(x => x.EndDate).HasColumnType("date");
        b.Entity<EventDay>().Property(x => x.Date).HasColumnType("date");
        b.Entity<PriceRule>().Property(x => x.EffectiveFrom).HasColumnType("date");
        b.Entity<PriceRule>().Property(x => x.EffectiveTo).HasColumnType("date");

        // Money precision
        b.Entity<PriceRule>().Property(x => x.Price).HasColumnType("decimal(6,2)");
        b.Entity<Sale>().Property(x => x.UnitPrice).HasColumnType("decimal(6,2)");
        b.Entity<Sale>().Property(x => x.DiscountValue).HasColumnType("decimal(6,2)");
        b.Entity<Expense>().Property(x => x.Amount).HasColumnType("decimal(6,2)");
        b.Entity<Payment>().Property(x => x.Amount).HasColumnType("decimal(6,2)");

        // Helpful indexes
        b.Entity<PriceRule>().HasIndex(r => new { r.ProductId, r.Price });
        b.Entity<Sale>().HasIndex(s => s.EventDayId);

        // That converter is needed to store Product.Brand as Bulgarian strings in the database
        // while keeping Brand as an enum in code.
        var brandToString = new ValueConverter<Brand, string>(
            v => v == Brand.Totem ? "ТОТЕМ"
               : v == Brand.Ceramics ? "Керамика"
               : v == Brand.Candles ? "Свещи"
               : v.ToString(),
            v => v == "ТОТЕМ" ? Brand.Totem
               : v == "Керамика" ? Brand.Ceramics
               : v == "Свещи" ? Brand.Candles
               : Brand.Totem
        );

        b.Entity<Product>()
            .Property(p => p.Brand)
            .HasConversion(brandToString)
            .HasMaxLength(50); // nvarchar(50) for SQL Server providers
    }
}
