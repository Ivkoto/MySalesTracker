# MySalesTracker

Blazor Server (.NET 9) application for tracking event sales with brand-based pricing rules. Built with Clean Architecture principles, using Entity Framework Core (SQL Server), SignalR for real-time updates, and a mobile-optimized UI.

## Quick start

#### Prerequisites

- .NET SDK 9.0+
- SQL Server (LocalDB or full SQL Server)
- A modern browser (mobile-friendly)

#### Configure database and SignalR

- Edit `MySalesTracker.Web/appsettings.json`:
  ```json
  {
    "ConnectionStrings": {
      "DatabaseConnection": "Server=(localdb)\\mssqllocaldb;Database=MySalesTracker;Trusted_Connection=True;MultipleActiveResultSets=true"
    },
    "SignalR": {
      "SalesHubPath": "/hubs/sales"
    }
  }
  ```

#### Run the app

- From the solution root:
  - Restore/build: `dotnet build`
  - Run: `dotnet run --project MySalesTracker.Web`
- Or F5 in Visual Studio with `MySalesTracker.Web` as startup project.

#### First run behavior

- The app automatically applies EF migrations and seeds initial data (brands, products, price rules).
- Navigate to:
  - `/events` to create and manage events/days
  - `/day/{id}` to add sales lines for a day
  - `/weather` to check weather forecast (Open-Meteo)

## Current features

- **Progressive Web App (PWA)** - installable on desktop and mobile devices
  - Add to home screen on Android/iOS
  - Opens in standalone mode (no browser UI)
  - App icons and splash screens
  - Requires HTTPS for mobile installation
- Events and event days
- Real-time sales sync across devices (SignalR push per event day)
- Sales entry per product with brand-based rules
  - `Керамика` (ceramics): manual price entry with decimal keypad on mobile
  - `Свещи/Гора` and `Тотем`: select price from rules; the selected price is the total for the units
- Discount input per sale (amount) with inline validation
- Grouped view by brand with daily totals
- Weather page (optional utility)
  - City search, 1 to 10 days forecast
  - Temperature and wind per hour
  - Color-coded hourly cards by temperature
  - State preserved within the same browser tab

## Architecture highlights

- **Dependency Inversion**: Application layer defines interfaces; Infrastructure implements them
- **Repository Pattern**: Data access abstracted through repository interfaces
- **Clean Separation**: Domain has zero dependencies on external frameworks
- **Testability**: Business logic can be tested without database or external APIs
- **Configuration-based**: SignalR hub paths and connection strings externalized to `appsettings.json`

## Development tips

- Hot Reload: edit Razor/C# and refresh; use F5 (Debug) for best experience.
- **PWA Testing**:
  - Desktop: Install directly from Chrome/Edge (install icon in address bar)
  - Mobile (local network): 
    - Run the app with LAN binding, e.g.: `dotnet run --project MySalesTracker.Web --urls http://0.0.0.0:5150`
    - Find your PC IP (e.g., 192.168.1.50), then open `http://192.168.1.50:5150` on your phone
    - Note: PWA installation on mobile requires HTTPS
  - Mobile (with HTTPS via ngrok):
    - Run your app locally
    - Start ngrok: `ngrok http 5000` (or your app's port)
    - Use the ngrok HTTPS URL on your phone to test PWA installation
    - Install from Chrome/Safari: "Add to Home Screen" or "Install app"
- Decimal keypad on mobile: inputs use `inputmode="decimal"` where needed.
- Icons: Bootstrap Icons are included via CDN in `Components/App.razor`.

## Data model

- `Brand` enum (stored as localized strings in DB via value converter)
- `Product` (Name, Brand, IsActive)
- `PriceRule` (Price, UnitsPerSale, SortOrder, Effective range)
- `Event` / `EventDay`
- `Sale` (Product snapshot, Price [total], QuantityUnits, DiscountValue, PriceRuleId, CreatedUtc)

## Migrations & seeding

- The app auto-migrates and seeds on startup (via `Program.cs`).
- Migrations are in `MySalesTracker.Infrastructure/Migrations/`
- To create a new migration:
  ```bash
  dotnet ef migrations add MigrationName --project MySalesTracker.Infrastructure --startup-project MySalesTracker.Web
  ```
- To start fresh:
  - Stop the app, drop the database
  - Delete migration files if needed
  - Run the app again to re-create and seed

## Project structure (Clean Architecture)

The project follows Clean Architecture principles with clear separation of concerns across four layers:

### `MySalesTracker.Domain` (Core Layer - Zero Dependencies)
- `Entities/` - Core business entities (`Event`, `EventDay`, `Sale`, `Product`, `PriceRule`, `Expense`, `Payment`)
- `Enums/` - Domain enumerations (`Brand`, `PaymentMethod`)
- `Models/` - Domain models and value objects (`BrandSalesSummary`, `UnitsPerSale`, `WeatherForecast`, `HourlyForecast`)
- `Services/` - Pure domain logic (`SalesCalculations`, `EventValidations`, `WeatherCalculations`)
- `Exceptions/` - Domain-specific exceptions

### `MySalesTracker.Application` (Use Cases Layer - Depends on Domain)
- `Interfaces/` - Service and repository contracts
  - `IEventRepository`, `ISaleRepository`, `IProductRepository`, `IPriceRuleRepository`
  - `IWeatherService`, `INotificationService`
- `Services/` - Application orchestration logic
  - `EventService`, `SaleService`, `ProductService`, `PriceRuleService`
- `Extensions/` - Dependency injection setup (`DependencyInjection.cs`)

### `MySalesTracker.Infrastructure` (External Concerns - Depends on Application + Domain)
- `Persistence/`
  - `AppDbContext.cs` - EF Core DbContext
  - `DataSeeder.cs` - Initial data seeding
  - `Repositories/` - Internal repository implementations
- `Migrations/` - EF Core migrations
- `ExternalServices/` - External API integrations
  - `WeatherService.cs` - Open-Meteo API client
  - `WeatherModels.cs` - External API DTOs
- `Services/` - Infrastructure service implementations
  - `SignalRNotificationService.cs` - Real-time notifications
- `Hubs/` - SignalR hubs (`SalesHub`)
- `Extensions/` - Infrastructure DI setup

### `MySalesTracker.Web` (Presentation Layer - Depends on Application + Infrastructure)
- `Components/`
  - `Pages/` - Razor pages (`Events.razor`, `EventDay.razor`, `Weather.razor`)
  - `Layout/` - Layout components (`MainLayout.razor`, `NavMenu.razor`)
  - `App.razor` - Main app component with PWA meta tags
- `State/` - UI state management (`WeatherState`)
- `Extensions/` - Helper extensions (`EnumExtensions`)
- `wwwroot/` - Static assets (CSS, JS, images)
  - `manifest.json` - PWA manifest configuration
  - `icons/` - PWA app icons (192x192, 512x512)
- `Program.cs` - Application entry point and DI composition root

## Troubleshooting

- **PWA not installing on mobile**: Ensure you're accessing the app via HTTPS (use ngrok for local testing or deploy to hosting)
- **PWA icons not showing**: Check that `wwwroot/icons/` contains all 4 PNG files (icon-192.png, icon-512.png, icon-192-maskable.png, and icon-512-maskable.png)
- **Missing SignalR configuration**: If you see `InvalidOperationException: SignalR:SalesHubPath is not configured`, ensure `appsettings.json` contains the `SignalR:SalesHubPath` setting.
- **EF decimal precision warnings**: The context configures money fields with precision; ensure migrations are up to date.
- **Missing Bootstrap icons**: Check the Bootstrap Icons CDN link in `Components/App.razor`.
- **Client error overlay**: Blazor shows `#blazor-error-ui` when a client/circuit error occurs.
- **CSS not loading**: Verify `MySalesTracker.Web.styles.css` reference in `App.razor` matches the generated scoped CSS bundle name.

## Branching Strategy

- `feature/*`: Individual feature branches
- `db/*`: Database related changes branch
- `bug/*`: Bug fix branches
- `fix/*`: Small fixes and refactoring branches

## Open-source component libraries for better visual experiance

- **Bootstrap components** https://icons.getbootstrap.com/

- **MudBlazor** https://www.mudblazor.com/ \
  Material-design look & feel, actively maintained, 70+ components (tables, dialogs, snackbars, autocomplete). Dark/light themes, responsive.

- **Radzen.Blazor** https://blazor.radzen.com/ \
  60+ free components, actively maintained, MIT license. DataGrid, Charts, Forms, Dialogs, Notifications.

_NuGet packages. Add, register their services in Program.cs, include their CSS/JS in wwwroot/index.html or \_Layout.cshtml and it's done._

#### For full enterprise-grade components (export to Excel, virtualization, charts):

- **Telerik UI for Blazor**
- **Syncfusion Blazor**
- **DevExpress Blazor**\
  Paid but come with support and all components.

---

Maintained for internal use. Contributions or suggestions are welcome.

