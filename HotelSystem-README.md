# 🏨 Hotel Channel Manager — Complete System

A full-stack hotel booking & channel management system built with **ASP.NET Core 8 + MySQL + HTML/CSS/JS**.

---

## 📁 Project Structure

```
hotel-system/
├── database/
│   └── schema.sql              ← Complete MySQL schema, stored procs, seed data
├── backend/
│   ├── HotelChannelManager.csproj
│   ├── Program.cs              ← App entry point, middleware, JWT, CORS
│   ├── appsettings.json        ← DB connection, JWT config
│   ├── Controllers/
│   │   └── Controllers.cs      ← All API controllers
│   ├── Models/
│   │   └── Models.cs           ← All entity models
│   ├── DTOs/
│   │   └── DTOs.cs             ← Request/response DTOs
│   └── Services/
│       ├── DatabaseService.cs  ← All DB operations (Dapper)
│       └── AuthService.cs      ← JWT token generation, BCrypt
└── frontend/
    ├── index.html              ← Public hotel booking website
    └── admin.html              ← Admin panel (login-gated)
```

---

## ⚙️ Setup Instructions

### 1. MySQL Database

```bash
# Login to MySQL
mysql -u root -p

# Run the schema file
source /path/to/hotel-system/database/schema.sql;
# OR
mysql -u root -proot123 < database/schema.sql
```

**Connection details:**
- Server: `localhost`
- Database: `HotelChannelManager`
- User: `root`
- Password: `root123`

---

### 2. ASP.NET Core Backend

**Requirements:** .NET 8 SDK ([download](https://dotnet.microsoft.com/download))

```bash
cd hotel-system/backend

# Restore packages
dotnet restore

# Run the API (port 5000)
dotnet run --urls=http://localhost:5000
```

The API will be available at:
- **API Base:** `http://localhost:5000/api`
- **Swagger UI:** `http://localhost:5000/swagger`
- **Health Check:** `http://localhost:5000/health`

---

### 3. Frontend (No build step required)

Open the HTML files directly in browser or serve with any static server:

```bash
# Option A: Python simple server
cd hotel-system/frontend
python -m http.server 8080

# Option B: VS Code Live Server extension
# Right-click index.html → Open with Live Server

# Option C: Direct file open
# Double-click index.html or admin.html
```

- **Hotel Booking Site:** `http://localhost:8080/index.html`
- **Admin Panel:** `http://localhost:8080/admin.html`

---

## 🔑 Default Login Credentials

| Username    | Password     | Role         |
|-------------|--------------|--------------|
| `admin`     | `Admin@2024` | SuperAdmin   |
| `manager`   | `Admin@2024` | HotelAdmin   |
| `frontdesk` | `Admin@2024` | FrontDesk    |

---

## 🏗️ System Features

### 🌐 Public Hotel Website (`index.html`)
- Luxury 5-star hotel booking interface
- Date-based room availability search
- Real-time pricing with GST calculation
- Multi-step booking form with guest details
- Online / Pay-at-Hotel payment options
- Booking tracking by reference number
- Guest-initiated booking cancellation

### 🖥️ Admin Panel (`admin.html`) — Login Required
All sections require login. After authentication with JWT, users can access:

| Module | Description |
|--------|-------------|
| **Dashboard** | Today's KPIs, occupancy, revenue by channel, recent bookings |
| **Bookings** | Full CRUD, search/filter, check-in, check-out, cancel |
| **New Booking** | Walk-in / desk booking with live pricing |
| **Rooms** | Room types & physical room management |
| **Rates & Pricing** | Bulk rate updates, daily/weekly overrides, default rates |
| **Availability** | Block/unblock rooms, calendar view |
| **Channel Partners** | 5 OTA partners, commission config, rate mappings |
| **Remittances** | Partner payment tracking & settlement |
| **Reports** | Booking report with revenue analysis |
| **Payments** | Payment transaction history |
| **Customers** | Guest database with search |
| **Users** | Staff user management with roles |
| **Settings** | Hotel profile & system configuration |

---

## 💳 Payment Modes

### Pay at Hotel (Guest pays on check-in)
- Guest books room, no upfront payment
- Reception collects payment during check-in
- Suitable for: Agoda, walk-in, hotel desk bookings

### Online Payment (Channel collects & remits)
- OTA collects payment from guest
- Hotel receives net amount (gross minus commission) after remittance cycle
- Suitable for: Booking.com, Expedia, MakeMyTrip, TripAdvisor

---

## 🔗 Channel Partners (Pre-configured)

| Partner | Code | Commission | Payment Mode | Remittance |
|---------|------|-----------|--------------|------------|
| Booking.com | BOOKING_COM | 15% | Online Collect | 30 days |
| Expedia | EXPEDIA | 18% | Online Collect | 30 days |
| MakeMyTrip | MMT | 12% | Online Collect | 15 days |
| Agoda | AGODA | 14% | Pay at Hotel | N/A |
| TripAdvisor | TRIPADVISOR | 10% | Online Collect | 30 days |

---

## 📡 API Reference

### Authentication
```
POST /api/auth/login          → Get JWT token
GET  /api/auth/me             → Current user info
POST /api/auth/change-password
```

### Bookings
```
POST /api/bookings/quote      → Get price quote (public)
POST /api/bookings            → Create booking
GET  /api/bookings            → List bookings (filtered)
GET  /api/bookings/{id}       → Get booking
GET  /api/bookings/reference/{ref} → Get by reference (public)
POST /api/bookings/{id}/cancel
POST /api/bookings/{id}/checkin
POST /api/bookings/{id}/checkout
```

### Rates & Availability
```
GET  /api/rates               → Get rates for date range
POST /api/rates/bulk          → Bulk update rates
GET  /api/rates/defaults      → Get default rates
POST /api/rates/defaults      → Save default rate
GET  /api/availability        → Get availability calendar
POST /api/availability/block  → Block rooms
POST /api/availability/unblock
```

### Channel Partners
```
GET  /api/channels            → List partners
POST /api/channels            → Create/update partner
GET  /api/channels/rate-mappings
POST /api/channels/rate-mappings
GET  /api/channels/remittances
POST /api/channels/remittances
```

### Reports
```
GET /api/reports/dashboard/{hotelId}
GET /api/reports/bookings
GET /api/reports/channels
GET /api/reports/occupancy
GET /api/reports/payments
GET /api/reports/remittances
```

---

## 🛡️ Role-Based Access

| Role | Permissions |
|------|------------|
| SuperAdmin | Full access to everything |
| HotelAdmin | All except user SuperAdmin actions |
| FrontDesk | Bookings, check-in/out, payments, availability |
| Reservations | Bookings, rates, availability |
| Finance | Payments, remittances, reports |
| ReportViewer | Read-only access to reports |

---

## 🗄️ Database Schema

**Key Tables:**
- `Hotels` — Hotel profile & policies
- `RoomTypes` — Room categories (Standard, Deluxe, Suite, Twin)
- `Rooms` — Physical room inventory
- `DefaultRoomRates` — Weekday/weekend fallback rates
- `RoomRates` — Daily rate overrides
- `RoomAvailability` — Inventory per date per room type
- `ChannelPartners` — OTA & partner configuration
- `ChannelRateMappings` — Per-channel markup/discount
- `Customers` — Guest profiles
- `Bookings` — Central reservation record
- `Payments` — Payment transactions
- `PartnerRemittances` — Channel settlement tracking
- `Users` — Staff accounts
- `AuditLogs` — All actions logged
- `SystemSettings` — Configurable hotel settings

**Key Stored Procedures:**
- `sp_GetEffectiveRate` — Calculate rate with channel markup
- `sp_CreateBooking` — Create booking with availability check
- `sp_CancelBooking` — Cancel with policy-based charge calculation

---

## 📦 NuGet Packages

```xml
MySql.Data          8.3.0   ← MySQL connector
Dapper              2.1.28  ← Micro-ORM for queries
BCrypt.Net-Next     4.0.3   ← Password hashing
JWT Bearer Auth     8.0.0   ← JWT authentication
Swashbuckle         6.5.0   ← Swagger/OpenAPI docs
Newtonsoft.Json     13.0.3  ← JSON serialization
```

---

## 🚀 Production Deployment Notes

1. **Change JWT secret** in `appsettings.json` — use a strong 64+ char key
2. **Change all default passwords** immediately after first login
3. **Enable HTTPS** — configure SSL in `Program.cs`
4. **Restrict CORS** — update allowed origins to your domain
5. **Set up MySQL backups** — schedule daily backups
6. **Configure logging** — Serilog to file/database
7. **Rate limiting** — add middleware for API protection

---

*Built for Hotel Channel Manager v1.0 | .NET 8 + MySQL + Vanilla HTML/CSS/JS*



****************************************************************************************************************
To build an Order Management System (OMS) for a Hotel Management System (HMS)

In hotels, orders usually include:

	Room Service Orders

	Restaurant Orders

	Laundry Orders

	Mini-bar consumption

	Service requests (chargeable)

	Add-ons linked to a booking

These orders must:

	Link to a Room

	Link to a Guest/Booking

	Generate a bill entry

	Update status

	Affect final checkout invoice