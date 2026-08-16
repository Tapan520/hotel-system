# Conventions & Code Style

## API Response Format
**Every** controller action returns `ApiResponse<T>`:
```json
{
  "success": true,
  "message": "Booking created: BK20260309123456",
  "data": { ... },
  "totalCount": 42,
  "timestamp": "2026-03-09T10:00:00"
}
```
- Use `ApiResponse<T>.Ok(data, message, total)` for success.
- Use `ApiResponse<T>.Fail(message, errorCode)` for errors.
- `totalCount` is set for paginated list responses.
- `errorCode` is a machine-readable string (e.g. `"INVALID_CREDENTIALS"`, `"DB_ERROR"`).
- **Never return raw objects** from controllers — always wrap in `ApiResponse<T>`.

## HTTP Status Codes Used
| Situation | Code |
|-----------|------|
| Success | 200 OK |
| Not found | 404 |
| Validation error / bad input | 400 |
| Unauthenticated | 401 (JSON, not HTML) |
| Forbidden | 403 (JSON, not HTML) |
| DB down | 503 |
| Unexpected server error | 500 |

## Error Handling Pattern
- Wrap controller actions in `try/catch`.
- Catch `MySqlException` separately to return `503` with `"DB_ERROR"` code.
- Catch `Exception` for `500` with `"SERVER_ERROR"` code.
- DatabaseService methods that call stored procedures return `string` messages prefixed:
  - `"SUCCESS: ..."` — operation succeeded
  - `"ERROR: ..."` — operation failed (business rule violation)
  - `"WARNING: ..."` — succeeded with a caveat (e.g. stock shortage)
- Controllers strip the prefix before returning to clients.

## Naming Conventions
| Thing | Convention | Example |
|-------|-----------|---------|
| C# classes / properties | PascalCase | `BookingId`, `GrandTotal` |
| JSON response fields | camelCase (auto via resolver) | `bookingId`, `grandTotal` |
| API routes | kebab-case | `/api/room-types`, `/api/booking-addons` |
| DB tables | lowercase snake_case | `bookings`, `room_types` |
| DB stored procs | `sp_PascalCase` | `sp_CreateBooking` |
| DB views | `vw_PascalCase` | `vw_Bookings`, `vw_OrderDetails` |

## Controller Structure Rules
- One controller per module, all in `Controllers/Controllers.cs`.
- Inject only `DatabaseService` (and `AuthService` where needed).
- Extract `UserId` from `ClaimTypes.NameIdentifier` — always safe, returns `0` if unauthenticated.
- Extract `HotelId` from custom claim `"HotelId"` — defaults to `1` if missing.
- Use `[AllowAnonymous]` explicitly on public endpoints inside `[Authorize]` controllers.

## DatabaseService Conventions
- One method per query/operation.
- Method names mirror the action: `GetBooking`, `CreateBooking`, `CancelBooking`, `UpdateBookingStatus`.
- Methods returning lists use `IEnumerable<T>` or `List<T>`.
- Methods returning a single item return `T?` (nullable).
- Methods that perform write operations and need to return IDs return `int` or `(int id, string message)`.
- Stored procedure calls return `(int id, string message)` or just `string message`.

## Audit Logging
- Call `_db.LogAudit(userId, action, table, referenceId, notes)` after every significant write operation.
- `action` is SCREAMING_SNAKE_CASE: `"CREATE_BOOKING"`, `"CANCEL_BOOKING"`, `"CHECKIN"`, etc.

## Frontend Conventions (admin.html / index.html)
- Vanilla JS — no build step, no framework.
- JWT stored in `localStorage` as `hotelToken` / `hotelUser`.
- All fetch calls check `d.success` (camelCase) — this is why the CamelCase resolver is critical.
- Error display: `showToast(message, type)` where type = `"success" | "error" | "warning"`.
