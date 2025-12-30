# FreeGLBA (Server Application)

Main server application for the FreeGLBA GLBA Compliance Data Access Tracking System. A Blazor Server application that hosts the web UI, APIs, and background services.

## Purpose

This is the main executable project that:
- **Hosts the Web UI** - Blazor Server interactive web application
- **Provides REST APIs** - External API for source systems, internal API for UI
- **Runs Background Services** - Scheduled tasks, cleanup, statistics
- **Manages Authentication** - Local, AD, OAuth, and OIDC support

## Technology Stack

- **.NET 10** - Latest .NET runtime
- **Blazor Server** - Interactive web UI with server-side rendering
- **Entity Framework Core** - Database access
- **SignalR** - Real-time notifications
- **Serilog** - Structured logging

## Dependencies

| Package | Purpose |
|---------|---------|
| `AspNet.Security.OAuth.Apple` | Apple Sign-In |
| `Microsoft.AspNetCore.Authentication.*` | OAuth providers |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | Enterprise SSO |
| `Microsoft.Azure.SignalR` | Azure SignalR Service |
| `Serilog.Extensions.Logging.File` | File-based logging |

## Running the Application

### Development

```bash
cd FreeGLBA
dotnet run
```

Navigate to `https://localhost:5001`

### Production

```bash
dotnet publish -c Release -o publish
# Deploy the publish folder to your server
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=FreeGLBA;Trusted_Connection=true;"
  },
  "DatabaseType": "SQLServer",
  
  "Authentication": {
    "Google": { "ClientId": "", "ClientSecret": "" },
    "Microsoft": { "ClientId": "", "ClientSecret": "" }
  },
  
  "BackgroundService": {
    "Enabled": true,
    "IntervalSeconds": 60
  }
}
```

## Key Folders

| Folder | Contents |
|--------|----------|
| `Components/` | Blazor components (App.razor, Routes.razor) |
| `Controllers/` | API controllers |
| `Middleware/` | Custom middleware (API key validation) |
| `Plugins/` | Plugin files (.cs, .plugin) |
| `wwwroot/` | Static files (CSS, JS, images) |

## API Endpoints

### External (API Key Auth)
- `POST /api/glba/events` - Submit access event
- `POST /api/glba/events/batch` - Submit batch

### Internal (User Auth)
- `GET /api/glba/stats/summary` - Dashboard stats
- `GET /api/Data/GetSourceSystems` - List sources
- `GET /api/Data/GetAccessEvents` - Query events

## Plugins

Place plugin files in the `/Plugins` folder:
- `.cs` files compile with the solution
- `.plugin` files load at runtime

See [FreeGLBA.Plugins/README.md](../FreeGLBA.Plugins/README.md) for details.

## Related Projects

- **FreeGLBA.Client** - Blazor WebAssembly UI
- **FreeGLBA.DataAccess** - Business logic
- **FreeGLBA.DataObjects** - DTOs
- **FreeGLBA.EFModels** - Database entities
- **FreeGLBA.Plugins** - Plugin system