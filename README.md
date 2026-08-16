# Hotel Channel Manager

ASP.NET Core 8 Hotel PMS & Channel Manager with MySQL backend.

## Railway Deployment

### Environment Variables to set in Railway (Backend service):
| Variable | Value |
|---|---|
| `MYSQL_PUBLIC_URL` | *(link from your Railway MySQL service)* |
| `JWT_KEY` | `HotelChannelManager_SuperSecretKey_2024_MustBe32CharsOrMore!` |

### URLs (after deploy):
- Frontend: `https://<your-app>.railway.app/`
- Admin: `https://<your-app>.railway.app/admin.html`
- Swagger: `https://<your-app>.railway.app/swagger`
- Health: `https://<your-app>.railway.app/health`

### Login credentials:
- admin / Admin@2024
- frontdesk / Admin@2024
- manager / Admin@2024

## Local Development
```bash
cd backend
dotnet restore
dotnet run --urls=http://localhost:5000
```
