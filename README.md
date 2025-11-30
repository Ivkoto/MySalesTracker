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
- Events and event days
- Real-time sales sync across devices (SignalR push per event day)
- Sales entry per product with brand-based rules
  - `Керамика` (ceramics): manual price entry with decimal keypad on mobile
  - `Свещи/Гора` and `Тотем`: select price from rules; the selected price is the total for the units
- Discount input per sale (amount) with inline validation
- Grouped view by brand with daily totals
- Daily sales summary by brands and available payments by payment method.
- Sales statistics by brands and payments available for the entire event.
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

**Payed libraries:**
- [Telerik UI for Blazor](https://www.telerik.com/blazor-ui)
- [Syncfusion Blazor](https://www.syncfusion.com/blazor-components)
- [DevExpress Blazor](https://www.devexpress.com/blazor/)

**Free libraries:**
- [Mud Blazor](https://mudblazor.com/getting-started/installation#online-playground)
---

Maintained for internal use. Contributions or suggestions are welcome.
