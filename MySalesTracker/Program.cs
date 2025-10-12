using Microsoft.EntityFrameworkCore;
using MySalesTracker.Components;
using MySalesTracker.Data;
using MySalesTracker.Hubs;
using MySalesTracker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .AddDbContextFactory<AppDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DatabaseConnection")));

// Business services
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<SaleService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<PriceRuleService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<WeatherService>();
builder.Services.AddScoped<WeatherState>();
builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var context = await dbFactory.CreateDbContextAsync();
    context.Database.Migrate();
    await DataSeeder.RunAsync(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapHub<SalesHub>(SalesHub.HubPath);

app.Run();
