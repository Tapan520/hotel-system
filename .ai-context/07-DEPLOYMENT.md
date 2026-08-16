# Deployment

## Platform
- **Railway** — Docker-based deployment
- One service: the .NET API (serves both API and static frontend HTML)
- One MySQL plugin: Railway-managed MySQL

## Docker Build
```dockerfile
# Build stage — uses .NET SDK 8
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Runtime stage — uses ASP.NET 8 (smaller image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
```
- `Dockerfile` is at repo root.
- Source path inside Docker: `backend/` is copied to `/src/`.
- Published output goes to `/app/publish`.
- Entry point: `dotnet HotelChannelManager.dll`.

## railway.toml
```toml
[build]
builder = "DOCKERFILE"
dockerfilePath = "Dockerfile"

[deploy]
startCommand = "dotnet HotelChannelManager.dll"
healthcheckPath = "/health"
healthcheckTimeout = 60
restartPolicyType = "ON_FAILURE"
restartPolicyMaxRetries = 3
```
- Health check: `GET /health` — must return 200. Also checks MySQL connectivity.

## Railway Environment Variables (Required)
| Variable | Description |
|----------|-------------|
| `PORT` | Injected by Railway automatically — do not set manually |
| `Jwt__Key` | JWT signing secret (min 32 chars recommended) |
| `Jwt__Issuer` | JWT issuer (default: `HotelChannelManager`) |
| `Jwt__Audience` | JWT audience (default: `HotelChannelManagerClients`) |
| `ConnectionStrings__DefaultConnection` | MySQL connection string |

> **.NET env var mapping**: Railway env vars with `__` (double underscore) map to nested config sections.  
> `Jwt__Key` ? `appsettings.json` section `Jwt.Key`  
> `ConnectionStrings__DefaultConnection` ? `ConnectionStrings.DefaultConnection`

## Local Development
```powershell
cd C:\CursorProjects\hotel-system\backend

dotnet clean
dotnet restore
dotnet build
dotnet run --urls=http://localhost:5000
```

### Local URLs
| Page | URL |
|------|-----|
| Public booking site | http://localhost:5000/ or http://localhost:5000/index.html |
| Admin PMS | http://localhost:5000/admin.html |
| Swagger | http://localhost:5000/swagger |
| Health | http://localhost:5000/health |
| API root | http://localhost:5000/api |

### Default Admin Credentials (local + Railway)
| Username | Password |
|----------|---------|
| `admin` | `Admin@2024` |
| `frontdesk` | `Admin@2024` |
| `manager` | `Admin@2024` |

## Database Setup (Fresh Instance)
1. Import `DataBase/Dump20260318.sql` into MySQL.
2. Update `appsettings.json` (or Railway env vars) with the connection string.
3. Start the app — it will test DB connectivity on startup.
4. Call `POST /api/availability/init` (authenticated as admin) to seed availability rows.

## Startup Diagnostics
On every startup, the app logs:
- JWT issuer, audience, and key hash (first 4 chars) — useful to detect misconfiguration.
- MySQL connection test result (`?` or `?`).
- All URLs and default credentials (printed to console/Railway logs).
