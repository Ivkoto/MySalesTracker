# MySalesTracker

Blazor Server (.NET 9) application for tracking event sales with brand-based pricing rules. Built with Clean Architecture principles, using Entity Framework Core (SQL Server), SignalR for real-time updates, .NET Aspire for local orchestration and observability, and a mobile-optimized UI.

## Quick start

#### Prerequisites

- .NET SDK 9.0+
- SQL Server (LocalDB or full SQL Server) OR Docker Desktop (for Aspire orchestration)
- A modern browser (mobile-friendly)

#### Run the app

**Option 1: Using .NET Aspire (Recommended for local development)**
- Set `MySalesTracker.AspireAppHost` as startup project in Visual Studio
- Press F5 or run: `dotnet run --project MySalesTracker.AspireAppHost`
- The Aspire dashboard opens automatically at `https://localhost:17157`
- SQL Server 2017 runs in Docker container (auto-downloaded if needed)
- Connection string is automatically provided by Aspire
- View logs, traces, and metrics in the Aspire dashboard

**Option 2: Direct run (Traditional approach)**
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
- From the solution root:
  - Restore/build: `dotnet build`
  - Run: `dotnet run --project MySalesTracker.Web`
- Or Ctrl + F5 in Visual Studio with `MySalesTracker.Web` as startup project.

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
- **.NET Aspire Orchestration** (local development)
  - Automatic SQL Server 2017 container management
  - Built-in observability dashboard with logs, traces, and metrics
  - OpenTelemetry integration for EF Core and SQL queries
  - Health checks and service discovery
- **Event Management**
  - Create events with date ranges
  - Auto-generate event days
  - Real-time sales sync across devices (SignalR push per event day)
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

- Weather page (optional utility)
  - City search, 1 to 10 days forecast
  - Temperature and wind per hour
  - Color-coded hourly cards by temperature
  - State preserved within the same browser tab

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


## Deployment

### IIS/Windows Hosting
1. Publish: `dotnet publish -c Release -o ./publish`
2. Ensure `web.config` is in the publish output root (not in wwwroot)
3. Configure IIS Application Pool to **"No Managed Code"**
4. Set `ASPNETCORE_ENVIRONMENT` in `web.config` (Production/Development)
5. Logs will be written to `logs/stdout_*.log` (one level above wwwroot)

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

**Paid libraries:**
- [Telerik UI for Blazor](https://www.telerik.com/blazor-ui)
- [Syncfusion Blazor](https://www.syncfusion.com/blazor-components)
- [DevExpress Blazor](https://www.devexpress.com/blazor/)

**Free libraries:**
- [Mud Blazor](https://mudblazor.com/getting-started/installation#online-playground)
---

Maintained for internal use. Contributions or suggestions are welcome.
