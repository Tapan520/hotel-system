# Key Decisions, Past Fixes & Gotchas

## Decision: Newtonsoft.Json over System.Text.Json
**Why**: The frontend JS uses `d.success`, `d.data.token`, `d.data.fullName` (camelCase).  
`System.Text.Json` with `JsonSerializerOptions.PropertyNamingPolicy = CamelCase` caused inconsistencies with complex nested types. `Newtonsoft.Json` + `CamelCasePropertyNamesContractResolver` is the reliable, proven fix.  
**Do NOT remove or change the `ContractResolver`** — login will silently break.

## Decision: AllowAnyOrigin (no AllowCredentials)
`AllowCredentials()` cannot be combined with `AllowAnyOrigin()` in ASP.NET Core — it throws at runtime. The hotel's frontend is served on the same origin as the API (same Railway service), so credentials in CORS are not needed.

## Decision: No EF Core
All queries are raw SQL via `MySql.Data`. This was a deliberate choice for performance and control over complex stored procedures. Do not introduce EF Core.

## Decision: DatabaseService as Singleton
`DatabaseService` opens/closes connections per-method using MySql.Data's connection pooling. It holds no per-request state, so Singleton is correct and safe.

## Fix: JWT 401 returning HTML instead of JSON
ASP.NET Core's default 401/403 behaviour redirected to a login page (HTML). Fixed by handling `OnChallenge` and `OnForbidden` events in `AddJwtBearer` to write JSON directly.  
**Do NOT remove those event handlers.**

## Fix: Login always failing despite correct credentials
Root cause: `ApiResponse<T>` properties were serialised as `{ "Success": true }` (PascalCase) but the JS checked `d.success` (camelCase) — so it was always `undefined` (falsy). Fixed by adding `CamelCasePropertyNamesContractResolver`. See the large comment block in `Program.cs`.

## Fix: Public booking creation
`POST /api/bookings` and `POST /api/bookings/quote` are `[AllowAnonymous]` so the public hotel website can create bookings without requiring guests to have an account.

## Fix: Railway PORT env var
Railway does not use port 5000. It injects `PORT` at runtime.  
`Program.cs` reads: `var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";`  
**Always use this pattern — never hardcode the port.**

## Fix: MySQL connection error at login returning HTML 500
A `MySqlException` during login caused an unhandled exception that produced an HTML 500 page, breaking `r.json()` in the frontend. Fixed by wrapping the login action in `try/catch` with separate `MySqlException` handler returning a clean 503 JSON.

## Gotcha: RoomAvailability must be initialised
On a fresh Railway deployment, the `roomavailability` table is empty, so all rooms show as unavailable. Admin must call `POST /api/availability/init` after setup to seed rows for the desired date range.

## Gotcha: JWT key via Railway env var
`appsettings.json` contains a placeholder JWT key for local dev.  
On Railway, the real key is set via env var `Jwt__Key` (double underscore maps to `Jwt:Key` in .NET config).  
If `Jwt__Key` is not set on Railway, the app **throws at startup** intentionally — this is by design.

## Gotcha: CustomerPortalController shares route prefix with CustomersController
Both are mapped to `api/customers`. This works because ASP.NET Core merges them by method. Be careful not to create conflicting routes when adding new endpoints to either controller.

## Gotcha: Recipe ? OrderCatalog sync
Creating or updating a Recipe **automatically creates/updates** the linked `ordercatalog` entry. If you modify `ordercatalog` directly, the Recipe's `SellingPrice` may go out of sync. Always update via the Recipe endpoint.

## Gotcha: Soft deletes everywhere
There are **no hard deletes** for business entities. Deactivation sets `IsActive = false`.  
Only `ordercatalog` items have a true delete via `DeleteCatalogItem` — this is by design for menu management.

## Gotcha: BillEntry voiding
Bill entries (folio lines) must never be deleted. Use `VoidBillEntry()` which sets `IsVoided = true` and records who voided it and why. The invoice total is recalculated excluding voided entries.
