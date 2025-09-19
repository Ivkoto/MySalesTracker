## Initial PMC commands

_Add initial migration:_

```
PM> Add-Migration Initial -Project MySalesTracker.Data -StartupProject MySalesTracker
```

_Update database:_

```
PM> Update-Database -Project MySalesTracker.Data -StartupProject MySalesTracker
```

## Mobile testing on device:

- Run the app with LAN binding
  `dotnet run --project MySalesTracker --urls http://0.0.0.0:5150`

- Or run the terminal in the 'MySalesTracker' folder
  `dotnet run --urls http://0.0.0.0:5150`

- Find your PC IP (e.g., 192.168.1.50), then open `http://192.168.1.50:5150/events` on your phone.
