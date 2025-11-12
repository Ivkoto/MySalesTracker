using Microsoft.EntityFrameworkCore;
using MySalesTracker.Application.Extensions;
using MySalesTracker.Infrastructure.Persistence;
using MySalesTracker.Infrastructure.Extensions;
using MySalesTracker.Web.Components;
using MySalesTracker.Web.State;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")))
    .SetApplicationName("MySalesTracker");

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.AddApplicationServices();
builder.Services.AddScoped<WeatherState>();

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
app.MapInfrastructureEndpoints(builder.Configuration);

app.Run();
