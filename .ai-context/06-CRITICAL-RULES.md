# ?? CRITICAL RULES — Read Before Making Any Changes

## ?? RULE #1 — NEVER DELETE PRODUCTION DATA
> **This rule is absolute. No exceptions. Ever.**

**The Railway/production MySQL database must NEVER have any existing data deleted.**

This applies to ALL of the following:
- Bookings (past, present, future — confirmed, cancelled, checked-out)
- Payments
- Customers / Guest profiles
- Audit logs
- Folio entries (BillEntry)
- Invoices (CheckoutInvoice)
- Orders and order items
- Stock movements
- Any other transactional or historical record

### What TO do instead of deleting:
| Instead of... | Do this... |
|---------------|-----------|
| Deleting a booking | Set `BookingStatus = 'Cancelled'` via the cancel flow |
| Deleting a customer | There is no delete — customer data is permanent |
| Deleting a payment | Payments are permanent records — add a correction entry if needed |
| Deleting a folio entry | Void it via `VoidBillEntry()` — sets `IsVoided = true` |
| Deleting a room type | Set `IsActive = false` (soft delete) |
| Deleting a channel partner | Set `IsActive = false` (soft delete) |
| Deleting a user | Set `IsActive = false` (soft delete) |
| Deleting an inventory item | Set `IsActive = false` via `DeactivateInventoryItem()` |
| Deleting a recipe | Set `IsActive = false` via `DeactivateRecipe()` |
| Deleting an add-on | Set `IsActive = false` via `DeactivateAddon()` |

### This rule applies to:
- All SQL migrations run against Railway
- All new API endpoints
- All code changes to existing endpoints
- Any database maintenance scripts
- Any manual SQL executed against Railway MySQL

---

## ?? RULE #2 — No Destructive Migrations on Production
- **Never run `DROP TABLE`, `TRUNCATE`, or `DELETE FROM` on the production Railway database.**
- Schema changes must be **additive**: `ALTER TABLE ... ADD COLUMN`, `CREATE TABLE`, `CREATE INDEX`.
- Before modifying a column, ensure existing data is preserved (e.g. add new column + migrate data, then optionally rename, never drop the old column until confirmed safe).

---

## ?? RULE #3 — Validate Before Writing to Production
- Always test SQL changes locally (against a local MySQL copy from `DataBase/Dump20260318.sql`) before applying to Railway.
- Never run untested SQL directly on the Railway database.

---

## ?? RULE #4 — Audit All Significant Writes
Every write that creates, modifies, or voids data must call `_db.LogAudit(userId, action, table, refId, notes)`.  
The audit log is a permanent record and must never be truncated or deleted.

---

## ?? RULE #5 — JWT Key Must Not Change in Production
Changing `Jwt__Key` on Railway invalidates **all active sessions** for all users.  
Only change it if absolutely necessary (security breach) and inform all users to re-login.

---

## ?? RULE #6 — Port Must Come from Environment Variable
Never hardcode the port number. Always use:
```csharp
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
```

---

## Summary Checklist for Any AI or Developer
Before deploying any change to Railway, confirm:
- [ ] No `DELETE FROM` or `TRUNCATE` statements on transactional tables
- [ ] No `DROP TABLE` or `DROP COLUMN` on existing tables
- [ ] All new features use soft-delete (`IsActive = false`) where applicable
- [ ] Folio entries are voided, not deleted
- [ ] Audit log calls added to all write operations
- [ ] Tested locally against a copy of the production schema
