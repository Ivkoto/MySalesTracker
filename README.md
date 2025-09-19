# MySalesTracker

Blazor Server (.NET 9) application for tracking event sales with brand-based pricing rules. Uses Entity Framework Core (SQL Server) and a simple UI optimized for mobile.

## Quick start

#### Prerequisites

- .NET SDK 9.0+
- SQL Server (LocalDB or full SQL Server)
- A modern browser (mobile-friendly)

#### Configure database

- Edit `MySalesTracker/appsettings.json` and set `ConnectionStrings:DatabaseConnection`.
  ```json
  {
    "ConnectionStrings": {
      "DatabaseConnection": "Server=(localdb)\\mssqllocaldb;Database=MySalesTracker;Trusted_Connection=True;MultipleActiveResultSets=true"
    }
  }
  ```

#### Run the app

- From the solution root:
  - Restore/build: `dotnet build`
  - Run: `dotnet run --project MySalesTracker/MySalesTracker.csproj`
- Or F5 in Visual Studio with `MySalesTracker` as startup project.

#### First run behavior

- The app automatically applies EF migrations and seeds initial data (brands, products, price rules).
- Navigate to:
  - `/events` to create and manage events/days
  - `/day/{id}` to add sales lines for a day
  - `/weather` to check weather forecast (Open?Meteo)

## Current features

- Events and event days
- Sales entry per product with brand-based rules
  - `Керамика` (ceramics): manual price entry with decimal keypad on mobile
  - `Свещи/Гора` and `Тотем`: select price from rules; the selected price is the total for the units
- Discount input per sale (amount)
- Grouped view by brand with daily totals
- Weather page (optional utility)
  - City search, 12/24/48-hour forecast
  - Temperature and wind per hour
  - Color-coded hourly cards by temperature
  - State preserved within the same browser tab

## Development tips

- Hot Reload: edit Razor/C# and refresh; use F5 (Debug) for best experience.
- Mobile testing on device:
  - Run the app with LAN binding, e.g.: `dotnet run --project MySalesTracker --urls http://0.0.0.0:5150`
  - Find your PC IP (e.g., 192.168.1.50), then open `http://192.168.1.50:5150/events` on your phone.
- Decimal keypad on mobile: inputs use `inputmode="decimal"` where needed.
- Icons: Bootstrap Icons are included via CDN in `Components/App.razor`.

## Data model

- `Brand` enum (stored as localized strings in DB via value converter)
- `Product` (Name, Brand, IsActive)
- `PriceRule` (Price, UnitsPerSale, SortOrder, Effective range)
- `Event` / `EventDay`
- `Sale` (Product snapshot, UnitPrice [total], QuantityUnits, DiscountValue, CreatedUtc)

## Migrations & seeding

- The app auto-migrates and seeds on startup. To start fresh:
  - Stop the app, drop the database, delete old migrations if needed
  - Run the app again to re-create and seed

## Project structure

- `MySalesTracker` (web app)
  - `Components/Pages` - pages (`Events.razor`, `EventDay.razor`, `Weather.razor`)
  - `Components/Layout` - layout and nav
  - `Services` - app services (e.g., `WeatherService`, `WeatherState`, `PriceRuleService`)
- `MySalesTracker.Data` (data access)
  - `Models` - entities and enums
  - `DataSeeder.cs` - initial data seeding
  - `Migrations` - EF Core migrations

## Troubleshooting

- EF decimal precision warnings: the context configures money fields with precision; ensure migrations are up to date.
- Missing icons: check the Bootstrap Icons link in `Components/App.razor`.
- Client error overlay: Blazor shows `#blazor-error-ui` when a client/circuit error occurs.

## Next moves / ideas

- Add Payments panel for Cash/Card/Revolut and show differences vs sales.
- Add Expenses for the day.
- Add Brand/Category summary page (your blue “Обороти” box).
- Make the day-entry UI touch-friendly (bigger buttons, number keypad, sticky totals).
- Add Admin CRUD for Products/Rules (so you can extend logic without code).

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
