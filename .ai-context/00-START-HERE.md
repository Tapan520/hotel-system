# AI Context — Hotel Channel Manager
> Read this file first. It points you to the right context file for each task.

## What is this project?
A full-stack **Hotel Property Management System (PMS) + Channel Manager** for **The Sapphire Suits**, a hotel in Mussoorie, India.

- **Backend**: ASP.NET Core 8 Web API (C#) — single deployable binary
- **Frontend**: Vanilla HTML + JS — served as static files from `backend/wwwroot/`
- **Database**: MySQL (hosted on Railway)
- **Deployment**: Docker on Railway (`railway.toml` + `Dockerfile`)

## Repository Layout
```
hotel-system/
??? backend/                         ? .NET 8 project (the entire API)
?   ??? Controllers/Controllers.cs   ? All API controllers in one file
?   ??? DTOs/DTOs.cs                 ? All request/response DTOs in one file
?   ??? Models/Models.cs             ? All domain models in one file
?   ??? Services/
?   ?   ??? DatabaseService.cs       ? All MySQL queries (massive file ~2 k lines)
?   ?   ??? AuthService.cs           ? JWT generation + BCrypt password hashing
?   ??? wwwroot/
?   ?   ??? index.html               ? Public hotel booking website
?   ?   ??? admin.html               ? Internal hotel PMS (staff only)
?   ??? Program.cs                   ? App startup, middleware pipeline
?   ??? appsettings.json             ? Local config (overridden by Railway env vars)
??? frontend/                        ? Source copies of HTML (mirror of wwwroot)
??? DataBase/Dump20260318.sql        ? Full MySQL schema + seed data
??? DatabaseService.cs               ? Root-level copy (reference / scratch)
??? Dockerfile                       ? Multi-stage build (sdk ? aspnet)
??? railway.toml                     ? Railway deployment config
??? Notes.txt                        ? Developer quick-reference notes
```

## Context Files Index
| File | What it covers |
|------|---------------|
| `01-ARCHITECTURE.md` | Tech stack, middleware order, auth flow, DB access pattern |
| `02-DOMAIN-AND-MODULES.md` | All business modules, models, and API routes |
| `03-BUSINESS-RULES.md` | Booking logic, pricing, cancellations, OMS, inventory |
| `04-CONVENTIONS.md` | Code style, response format, naming, error handling |
| `05-DECISIONS-AND-GOTCHAS.md` | Key past decisions, known fixes, traps to avoid |
| `06-CRITICAL-RULES.md` | ?? MUST-READ safety rules — especially production data protection |
| `07-DEPLOYMENT.md` | Railway env vars, Docker, local dev commands |
