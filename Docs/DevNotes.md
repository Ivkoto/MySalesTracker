## Development Setup

### Tools Installation
```bash
# Restore local tools (dotnet-ef)
dotnet tool restore
```

### Running with .NET Aspire

**Start the AppHost:**
```bash
dotnet run --project MySalesTracker.AspireAppHost
```

**Or in Visual Studio:**
- Set `MySalesTracker.AspireAppHost` as startup project
- Press F5

**What happens:**
- Aspire dashboard opens at `https://localhost:17157`
- SQL Server 2017 container starts automatically (Docker required)
- Connection string `sql2017` is injected into the web app
- OpenTelemetry traces, metrics, and logs are collected
- View real-time EF Core queries, SQL commands, and HTTP requests in dashboard

**Aspire Dashboard Features:**
- **Resources**: View status of SQL Server container and web app
- **Console Logs**: See application logs with filtering
- **Structured Logs**: Query logs with full context
- **Traces**: Visualize request flows across EF Core and SQL
- **Metrics**: Monitor app performance (request duration, SQL query count, etc.)

### EF Core Migrations

_Add new migration:_
```bash
dotnet ef migrations add MigrationName --project MySalesTracker.Infrastructure --startup-project MySalesTracker.Web
```

_Update database:_
```bash
dotnet ef database update --project MySalesTracker.Infrastructure --startup-project MySalesTracker.Web
```

_Remove last migration:_
```bash
dotnet ef migrations remove --project MySalesTracker.Infrastructure --startup-project MySalesTracker.Web
```

### Mobile Testing on Device

**Option 1: Direct run (without Aspire)**
- Run the app with LAN binding:
  ```bash
  dotnet run --project MySalesTracker.Web --urls http://0.0.0.0:5150
  ```
- Find your PC IP (e.g., 192.168.1.50), then open `http://192.168.1.50:5150` on your phone.

**Option 2: Using Aspire Dev Tunnels (HTTPS support)**
- Dev tunnels are configured in `AppHost.cs` for external HTTPS access
- When you run the AppHost, a secure tunnel is automatically created
- Check the Aspire dashboard for the public HTTPS URL
- Use this URL on your mobile device (supports PWA installation)

## PWA Testing
- Desktop: Install directly from Chrome/Edge (install icon in address bar)
- Mobile (local network): 
  - Run the app with LAN binding, e.g.: `dotnet run --project MySalesTracker.Web --urls http://0.0.0.0:5150`.
  - Find your PC IP (e.g., 192.168.1.50), then open `http://192.168.1.50:5150` on your phone.
  - Note: PWA installation on mobile requires HTTPS.
- Mobile (with HTTPS via ngrok):
  - Run your app locally.
  - Start ngrok: `ngrok http 5000` (or your app's port).
  - Use the ngrok HTTPS URL on your phone to test PWA installation.
  - Install from Chrome/Safari: "Add to Home Screen" or "Install app".

## Production Deployment Notes

### IIS Configuration (`web.config`)
- Log files are written to parent folder: `..\logs\stdout`.
- Environment set to `Production`.
- In-process hosting model for better performance.

### Database Connection
- **With Aspire**: Connection string is automatically provided via service reference `sql2017`
- **Without Aspire**: Falls back to `DatabaseConnection` from `appsettings.json`
- Connection string resolution in `DependencyInjection.cs` checks Aspire first, then local config

### Data Protection Keys
- Keys stored in `DataProtection-Keys/` folder in app root.
- Application name set to `MySalesTracker` for consistency across deployments.
- Prevents permission issues with system profile folder on shared hosting.

### SignalR Configuration
- Hub path configurable via `appsettings.json`: `SignalR:SalesHubPath`.
- Supports WebSockets, Server-Sent Events, and Long Polling transports.
- Timeouts increased for mobile/slow connections.

### SEO & Security
- `robots.txt` disallows all crawlers (private app).
- `<meta name="robots" content="noindex, nofollow">` in App.razor.
- Production connection strings in `appsettings.Production.json` (gitignored).
- Publish profiles excluded from repository.
