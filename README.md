# MySalesTracker

Blazor Server (.NET 10) application for tracking event sales with brand-based pricing rules. Built with Clean Architecture principles, using Entity Framework Core (SQL Server), SignalR for real-time updates, .NET Aspire for local orchestration and observability, and a mobile-optimised UI.

## Quick start

#### Prerequisites

- .NET SDK 10
- SQL Server (LocalDB or full SQL Server) OR Docker Desktop (for Aspire orchestration)
- A modern browser (mobile-friendly)

#### Run the app

- Restore repository-local tools from the solution root: `dotnet tool restore`
- The development login PIN is configured as `AuthPin:Pin` in `MySalesTracker.Web/appsettings.Development.json` for both startup options.
- Use HTTPS because authentication cookies are configured for secure connections.

**Option 1: Using .NET Aspire (Recommended for local development)**

- Set `MySalesTracker.AspireAppHost` as a startup project in Visual Studio
- Press F5 or run: `dotnet run --project MySalesTracker.AspireAppHost`
- The Aspire dashboard opens automatically at `https://localhost:17157`
- SQL Server 2017 runs in a Docker container (auto-downloaded if needed)
- Connection string is automatically provided by Aspire
- View logs, traces, and metrics in the Aspire dashboard

**Option 2: Direct run (Traditional approach)**

- Configure the database connection and login PIN in `MySalesTracker.Web/appsettings.Development.json`:
  ```json
  {
    "ConnectionStrings": {
      "DatabaseConnection": "Server=(localdb)\\mssqllocaldb;Database=MySalesTracker;Trusted_Connection=True;MultipleActiveResultSets=true"
    },
    "AuthPin": {
      "Pin": "<your-pin>"
    }
  }
  ```
- From the solution root:
  - Restore/build: `dotnet build MySalesTracker.Web/MySalesTracker.Web.csproj`
  - Run: `dotnet run --project MySalesTracker.Web --launch-profile https`
- Or Ctrl + F5 in Visual Studio with `MySalesTracker.Web` as startup project and the `https` launch profile selected.

#### First run behaviour

- The app automatically applies pending EF migrations.
- For an empty database, populate the product catalogue separately, then configure current prices at `/admin/prices`.
- Navigate to:
  - `/` for the home weather overview and access to events
  - `/events` to create and manage events/days
  - `/day/{id}` to add sales lines for a day
  - `/admin/prices` to manage product prices and defaults
  - `/weather` to check weather forecast (Open-Meteo)

## Current features

- **Home page**
  - Clickable weather overview showing the selected city, current temperature, wind, daily minimum and maximum, and rain outlook
  - Morning, afternoon, and evening temperatures
  - Animated cash-deposit illustration linking to events, with a static version for reduced-motion preferences
- **Progressive Web App (PWA)** - installable on desktop and mobile devices
  - Add to home screen on Android/iOS
  - Opens in standalone mode (no browser UI)
  - App icons and splash screens
  - Requires HTTPS for mobile installation
- **.NET Aspire Orchestration** (local development)
  - Automatic SQL Server 2017 container management
  - Built-in observability dashboard with logs, traces, and metrics
  - OpenTelemetry integration for EF Core and SQL queries
  - Health checks and service discovery
- **Event Management**
  - Create events with date ranges
  - Auto-generate event days
  - Expand the two events with the latest end dates by default when the page opens
  - Expand or collapse event cards independently
  - Real-time sales sync across devices (SignalR push per event day)
- **Price Management**
  - Manage current product prices at `/admin/prices`, grouped by brand
  - Add, edit, and remove prices, set units per sale, and choose one default price per product
  - Keep ceramics as free-price products
  - Preserve the price recorded on historical sales when current prices change
- **Sales Entry**
  - Sales entry per product with brand-based rules
  - `Керамика` (ceramics): manual price entry with decimal keypad on mobile
  - `Свещи/Гора` and `Тотем`: select price from rules; the selected price is the total for the units
  - Discount input per sale (amount) with inline validation
  - Edit and delete sales
  - Grouped view by brand with daily totals
- **Statistics & Reports**
  - Daily sales summary by brands and payment methods
  - Event summary with aggregated statistics across all days
  - **Compare Days**: Select specific days from an event to compare side-by-side (cards or table view)
  - **Compare Events**: Select multiple events to compare revenue, counts, and payments
  - Product-level breakdowns for Totem and Gora brands (expandable in UI)
  - Query string persistence for sharing specific comparisons

- **Weather forecast**
  - Open-Meteo forecasts for 1 to 10 days, with 3 days selected by default
  - City search and current-location selection with browser permission
  - Daily and hourly temperature, wind, rain probability, and precipitation amounts
  - Colour-coded bars showing hourly rainfall amounts
  - Position today's hourly forecast at the selected city's current hour and keep the current-hour marker updated
  - Preserve the city, location coordinates, and selected forecast length across navigation during the current app session
  - Cache forecasts for 15 minutes and share cached data with the home weather overview

## Migrations & seeding

- The app applies migrations on startup through `DatabaseInitializer`, called from `Program.cs`.
- Automatic product seeding is currently disabled. `DataSeeder.RunAsync` defines starter products only, without price rules.
- Migrations are in `MySalesTracker.Infrastructure/Migrations/`
- To create a new migration:
  ```bash
  dotnet ef migrations add MigrationName --project MySalesTracker.Infrastructure --startup-project MySalesTracker.Web
  ```
- To recreate a disposable development database:
  - Stop the app and drop the development database
  - Keep the checked-in migration files and model snapshot
  - Restart the app to recreate the schema, then populate the product catalogue and configure prices

## Testing

The `Tests` project uses xUnit.net v3, ArchUnitNET architecture checks, and the Visual Studio test adapter. It references `xunit.v3.mtp-off` to retain VSTest and `coverlet.collector` support on .NET 10.

Run the tests from the solution root:

```bash
dotnet test Tests/Tests.csproj
```

## Deployment

### IIS/Windows Hosting

1. Publish: `dotnet publish MySalesTracker.Web/MySalesTracker.Web.csproj -c Release -o ./publish`
2. Ensure `web.config` is in the publish output root (not in wwwroot)
3. Configure IIS Application Pool to **"No Managed Code"**
4. Set `ASPNETCORE_ENVIRONMENT` in `web.config` (Production/Development)
5. Logs will be written to `..\logs\stdout*.log`, relative to the deployed application root, as configured in `web.config`

### Data Protection Keys
The app stores encryption keys in the `DataProtection-Keys/` folder (not in the system profile). This prevents permission issues on shared hosting. Each environment should generate its own keys automatically on first run.

### SignalR Configuration
- Configure hub path in `appsettings.json`: `SignalR:SalesHubPath`
- Supports multiple transports: WebSockets, Server-Sent Events, Long Polling
- Optimized timeouts for mobile connections

### Security & SEO
- **Private app**: `robots.txt` and `noindex` meta tag prevent search engine crawling
- **Secrets**: `appsettings.Production.json` is gitignored and must be created manually on server
- **Publish profiles**: Excluded from repository to protect deployment credentials

## Architecture & Project Structure

- **MySalesTracker.Domain** - Entities, enums, domain models, and pure business logic
- **MySalesTracker.Application** - Services, DTOs, interfaces (application layer)
- **MySalesTracker.Infrastructure** - EF Core repositories, external services, SignalR hubs
- **MySalesTracker.Web** - Blazor Server UI, pages, components
- **MySalesTracker.AspireAppHost** - .NET Aspire orchestration host
- **MySalesTracker.AspireServiceDefaults** - Shared Aspire telemetry and service configuration
- **Tests** - xUnit.net v3 tests for architecture, pricing, and weather behavior

## Branching Strategy

- `feature/*`: Individual feature branches
- `db/*`: Database-related changes branch
- `bug/*`: Bug fix branches
- `fix/*`: Small fixes and refactoring branches

## UI components and assets

- Bootstrap provides the base styles and utility classes.
- Bootstrap Icons supplies icons for navigation, forms, and weather.
- Custom Blazor components, CSS, and SVG illustrations provide the application UI and home-page animation.
- Shared styles and scripts are loaded in `MySalesTracker.Web/Components/App.razor`.

---

Maintained for internal use. Contributions or suggestions are welcome.
