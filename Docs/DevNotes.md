## Development Setup

### Tools Installation
```bash
# Restore local tools (dotnet-ef)
dotnet tool restore
```

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

- Run the app with LAN binding:
  ```bash
  dotnet run --project MySalesTracker.Web --urls http://0.0.0.0:5150
  ```

- Find your PC IP (e.g., 192.168.1.50), then open `http://192.168.1.50:5150` on your phone.

## PWA Testing
- Desktop: Install directly from Chrome/Edge (install icon in address bar)
- Mobile (local network): 
  - Run the app with LAN binding, e.g.: `dotnet run --project MySalesTracker.Web --urls http://0.0.0.0:5150`
  - Find your PC IP (e.g., 192.168.1.50), then open `http://192.168.1.50:5150` on your one
  - Note: PWA installation on mobile requires HTTPS
- Mobile (with HTTPS via ngrok):
  - Run your app locally
  - Start ngrok: `ngrok http 5000` (or your app's port)
  - Use the ngrok HTTPS URL on your phone to test PWA installation
  - Install from Chrome/Safari: "Add to Home Screen" or "Install app"

## Production Deployment Notes

### IIS Configuration (`web.config`)
- Log files are written to parent folder: `..\logs\stdout`
- Environment set to `Production`
- In-process hosting model for better performance

### Data Protection Keys
- Keys stored in `DataProtection-Keys/` folder in app root
- Application name set to `MySalesTracker` for consistency across deployments
- Prevents permission issues with system profile folder on shared hosting

### SignalR Configuration
- Hub path configurable via `appsettings.json`: `SignalR:SalesHubPath`
- Supports WebSockets, Server-Sent Events, and Long Polling transports
- Timeouts increased for mobile/slow connections.

### SEO & Security
- `robots.txt` disallows all crawlers (private app)
- `<meta name="robots" content="noindex, nofollow">` in App.razor
- Production connection strings in `appsettings.Production.json` (gitignored)
- Publish profiles excluded from repository
