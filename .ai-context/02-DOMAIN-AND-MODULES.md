# Domain Modules & API Routes

## Module Overview
| Module | Route Prefix | Controller | Auth Required |
|--------|-------------|-----------|---------------|
| Auth | `/api/auth` | `AuthController` | Login = public |
| Hotels | `/api/hotels` | `HotelsController` | GET = public |
| Room Types | `/api/room-types` | `RoomTypesController` | GET = public |
| Rooms | `/api/rooms` | `RoomsController` | Yes |
| Rates | `/api/rates` | `RatesController` | Yes |
| Availability | `/api/availability` | `AvailabilityController` | GET = public |
| Channel Partners | `/api/channels` | `ChannelsController` | Yes |
| Customers | `/api/customers` | `CustomersController` + `CustomerPortalController` | Search = staff; portal = Customer JWT |
| Booking Add-ons | `/api/booking-addons` | `BookingAddonsController` | GET = public |
| Bookings | `/api/bookings` | `BookingsController` | Create/Quote = public |
| Payments | `/api/payments` | `PaymentsController` | Yes |
| Reports | `/api/reports` | `ReportsController` | Yes |
| Users | `/api/users` | `UsersController` | SuperAdmin/HotelAdmin |
| Settings | `/api/settings` | `SettingsController` | SuperAdmin/HotelAdmin |
| Orders (OMS) | `/api/orders` | `OrdersController` | Yes |
| Inventory | `/api/inventory` | `InventoryController` | Yes |
| Recipes | `/api/recipes` | `RecipesController` | Yes |

---

## Key Models (Models.cs)
| Model | Description |
|-------|-------------|
| `Hotel` | Single hotel record (multi-hotel ready via HotelId FK) |
| `RoomType` | Category of room (e.g. Deluxe, Suite) |
| `Room` | Physical room with number, floor, status |
| `DefaultRoomRate` | Base weekday/weekend rates per room type |
| `RoomRate` | Per-date rate overrides |
| `RoomAvailability` | Daily inventory: TotalRooms - BlockedRooms - BookedRooms |
| `ChannelPartner` | OTA / corporate booking partner |
| `ChannelRateMapping` | Markup % per partner per room type |
| `Customer` | Guest profile (persisted across bookings) |
| `Booking` | Core booking record with all financials |
| `BookingAddonCatalog` | Menu of extras (breakfast, extra bed, etc.) |
| `BookingAddonItem` | Addon lines attached to a booking |
| `Payment` | Payment transactions against a booking |
| `PartnerRemittance` | Commission settlement records per partner |
| `User` | Staff user with role and hotel assignment |
| `DashboardStats` | Aggregated dashboard data (not persisted) |
| `Order` | OMS order (InRoom or DirectSale) |
| `OrderItem` | Line items within an order |
| `OrderCatalogItem` | Restaurant/bar menu (linked to Recipes via CatalogId) |
| `BillEntry` | Folio line posted to a booking |
| `CheckoutInvoice` | Final checkout invoice |
| `InventoryItem` | Ingredient / supply master record |
| `Recipe` | Recipe linked to an OrderCatalog item |
| `RecipeIngredient` | Ingredient lines within a recipe |
| `StockMovement` | IN/OUT/ADJUSTMENT/WASTE movements |

---

## Notable Public (Anonymous) Endpoints
These require **no JWT** — used by the public booking website (`index.html`):
- `GET  /api/hotels`
- `GET  /api/room-types`
- `GET  /api/availability`
- `GET  /api/booking-addons`
- `POST /api/bookings/quote`
- `POST /api/bookings`
- `GET  /api/bookings/reference/{ref}`
- `POST /api/bookings/{id}/cancel`
- `POST /api/customers/login` (Customer Portal)

---

## Special Endpoints
| Endpoint | Purpose |
|----------|---------|
| `GET  /health` | DB connectivity check — used by Railway health probe |
| `POST /api/availability/init` | Bulk-seed availability rows on fresh DB (admin only) |
| `POST /api/orders/{id}/quickbill` | Single-step Delivered + Billed for DirectSale orders |
| `POST /api/recipes/deduct-stock` | Manually deduct ingredients for N portions |
| `GET  /api/recipes/{id}/stock-check` | Check if stock is sufficient for N portions |
| `GET  /api/customers/my-bookings` | Customer Portal — all bookings by email (Customer JWT) |
