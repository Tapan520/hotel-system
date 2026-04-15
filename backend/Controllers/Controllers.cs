using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelChannelManager.DTOs;
using HotelChannelManager.Models;
using HotelChannelManager.Services;

namespace HotelChannelManager.Controllers
{
    // ── AUTH CONTROLLER ────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly DatabaseService _db;
        private readonly AuthService _auth;

        public AuthController(DatabaseService db, AuthService auth)
        {
            _db = db;
            _auth = auth;
        }

        // FIX: Safe UserId that doesn't throw for unauthenticated calls
        private int UserId
        {
            get
            {
                var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(v, out var id) ? id : 0;
            }
        }

        [HttpPost("login")]
        // FIX: Wrapped entire login in try/catch so a MySQL error returns a clean
        // JSON error instead of a 500 HTML page that breaks admin.html's r.json() call.
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail("Username and password are required"));

            try
            {
                var user = await _db.GetUserByUsername(req.Username);

                if (user == null)
                    return Unauthorized(
                        ApiResponse<string>.Fail("Invalid username or password", "INVALID_CREDENTIALS"));

                if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
                    return Unauthorized(
                        ApiResponse<string>.Fail(
                            $"Account locked until {user.LockedUntil:HH:mm}. Try again later.",
                            "ACCOUNT_LOCKED"));

                if (!AuthService.VerifyPassword(req.Password, user.PasswordHash))
                {
                    try { await _db.IncrementLoginAttempts(req.Username); } catch { }
                    return Unauthorized(
                        ApiResponse<string>.Fail("Invalid username or password", "INVALID_CREDENTIALS"));
                }

                try { await _db.UpdateLastLogin(user.UserId); } catch { }
                try { await _db.LogAudit(user.UserId, "LOGIN", "Auth", null,
                    $"Login from {HttpContext.Connection.RemoteIpAddress}"); } catch { }

                return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse
                {
                    Token     = _auth.GenerateToken(user),
                    Username  = user.Username,
                    FullName  = user.FullName ?? user.Username,
                    Role      = user.Role,
                    HotelId   = user.HotelId,
                    ExpiresAt = DateTime.UtcNow.AddHours(10)
                }));
            }
            catch (MySql.Data.MySqlClient.MySqlException mysqlEx)
            {
                // FIX: Return a clear JSON error when MySQL is down/unreachable
                return StatusCode(503, ApiResponse<string>.Fail(
                    $"Database connection failed: {mysqlEx.Message}. " +
                    "Ensure MySQL is running and credentials are correct.",
                    "DB_ERROR"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail(
                    $"Login error: {ex.Message}", "SERVER_ERROR"));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            try
            {
                var user = await _db.GetUserByUsername(User.Identity!.Name!);
                if (user == null) return NotFound();
                if (!AuthService.VerifyPassword(req.CurrentPassword, user.PasswordHash))
                    return BadRequest(ApiResponse<string>.Fail("Current password is incorrect"));
                await _db.UpdateUserPassword(UserId, AuthService.HashPassword(req.NewPassword));
                return Ok(ApiResponse<string>.Ok("Password changed successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail(ex.Message));
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var user = await _db.GetUserByUsername(User.Identity!.Name!);
            return Ok(ApiResponse<object>.Ok(new
            {
                user?.UserId, user?.Username, user?.FullName,
                user?.Email, user?.Role, user?.HotelId, user?.LastLoginAt
            }));
        }
    }

    // ── HOTELS CONTROLLER ──────────────────────────────────────────────────────
    [ApiController]
    [Route("api/hotels")]
    [Authorize]
    public class HotelsController : ControllerBase
    {
        private readonly DatabaseService _db;
        public HotelsController(DatabaseService db) => _db = db;

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> List()
            => Ok(ApiResponse<object>.Ok(await _db.GetHotels()));

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var h = await _db.GetHotel(id);
            return h == null
                ? NotFound(ApiResponse<string>.Fail("Hotel not found"))
                : Ok(ApiResponse<Hotel>.Ok(h));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Save([FromBody] Hotel h)
        {
            var id = await _db.SaveHotel(h);
            return Ok(ApiResponse<int>.Ok(id, h.HotelId == 0 ? "Hotel created" : "Hotel updated"));
        }
    }

    // ── ROOM TYPES CONTROLLER ──────────────────────────────────────────────────
    [ApiController]
    [Route("api/room-types")]
    public class RoomTypesController : ControllerBase
    {
        private readonly DatabaseService _db;
        public RoomTypesController(DatabaseService db) => _db = db;

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> List([FromQuery] int? hotelId = 1)
            => Ok(ApiResponse<object>.Ok(await _db.GetRoomTypes(hotelId)));

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var rt = await _db.GetRoomType(id);
            return rt == null ? NotFound() : Ok(ApiResponse<RoomType>.Ok(rt));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> Save([FromBody] RoomType rt)
        {
            var id = await _db.SaveRoomType(rt);
            return Ok(ApiResponse<int>.Ok(id,
                rt.RoomTypeId == 0 ? "Room type created" : "Room type updated"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var rt = await _db.GetRoomType(id);
            if (rt == null) return NotFound();
            rt.IsActive = false;
            await _db.SaveRoomType(rt);
            return Ok(ApiResponse<string>.Ok("Room type deactivated"));
        }
    }

    // ── ROOMS CONTROLLER ───────────────────────────────────────────────────────
    [ApiController]
    [Route("api/rooms")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly DatabaseService _db;
        public RoomsController(DatabaseService db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] int? hotelId = 1, [FromQuery] int? roomTypeId = null)
            => Ok(ApiResponse<object>.Ok(await _db.GetRooms(hotelId, roomTypeId)));

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HotelAdmin,FrontDesk")]
        public async Task<IActionResult> Save([FromBody] Room r)
        {
            var id = await _db.SaveRoom(r);
            return Ok(ApiResponse<int>.Ok(id, r.RoomId == 0 ? "Room created" : "Room updated"));
        }
    }

    // ── RATES CONTROLLER ───────────────────────────────────────────────────────
    [ApiController]
    [Route("api/rates")]
    [Authorize]
    public class RatesController : ControllerBase
    {
        private readonly DatabaseService _db;
        public RatesController(DatabaseService db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetRates(
            [FromQuery] int roomTypeId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var rates = await _db.GetRates(
                roomTypeId, from ?? DateTime.Today, to ?? DateTime.Today.AddDays(60));
            return Ok(ApiResponse<object>.Ok(rates));
        }

        [HttpPost("bulk")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin,Reservations")]
        public async Task<IActionResult> BulkUpdate([FromBody] BulkRateRequest req)
        {
            if (req.ToDate < req.FromDate)
                return BadRequest(ApiResponse<string>.Fail("End date must be >= start date"));
            await _db.BulkSaveRates(req);
            return Ok(ApiResponse<string>.Ok("Rates updated successfully"));
        }

        [HttpGet("defaults")]
        public async Task<IActionResult> GetDefaults([FromQuery] int? roomTypeId)
            => Ok(ApiResponse<object>.Ok(await _db.GetDefaultRates(roomTypeId)));

        [HttpPost("defaults")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin,Reservations")]
        public async Task<IActionResult> SaveDefault([FromBody] DefaultRoomRate dr)
        {
            var id = await _db.SaveDefaultRate(dr);
            return Ok(ApiResponse<int>.Ok(id, "Default rate saved"));
        }
    }

    // ── AVAILABILITY CONTROLLER ────────────────────────────────────────────────
    [ApiController]
    [Route("api/availability")]
    public class AvailabilityController : ControllerBase
    {
        private readonly DatabaseService _db;
        public AvailabilityController(DatabaseService db) => _db = db;

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get(
            [FromQuery] int? roomTypeId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var avail = await _db.GetAvailability(
                roomTypeId, from ?? DateTime.Today, to ?? DateTime.Today.AddDays(30));
            return Ok(ApiResponse<object>.Ok(avail));
        }

        /// <summary>
        /// POST /api/availability/init
        /// Admin-triggered bulk initialisation of roomavailability rows.
        /// Call this once after setting up room types and rooms on a fresh DB
        /// so the frontend shows correct availability instead of all-Unavailable.
        /// Body: { "fromDate": "2026-01-01", "toDate": "2026-12-31", "roomTypeId": null }
        /// roomTypeId is optional — omit (or null) to init ALL room types.
        /// </summary>
        [HttpPost("init")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> Init([FromBody] InitAvailabilityRequest req)
        {
            if (req.ToDate < req.FromDate)
                return BadRequest(ApiResponse<string>.Fail("toDate must be >= fromDate"));
            if ((req.ToDate - req.FromDate).TotalDays > 730)
                return BadRequest(ApiResponse<string>.Fail("Date range cannot exceed 2 years"));

            await _db.InitAvailability(req.RoomTypeId, req.FromDate, req.ToDate);
            return Ok(ApiResponse<string>.Ok(
                $"Availability initialised from {req.FromDate:yyyy-MM-dd} to {req.ToDate:yyyy-MM-dd}"));
        }

        [HttpPost("block")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin,FrontDesk,Reservations")]
        public async Task<IActionResult> Block([FromBody] BlockAvailabilityRequest req)
        {
            await _db.BlockAvailability(req);
            return Ok(ApiResponse<string>.Ok("Rooms blocked"));
        }

        [HttpPost("unblock")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin,FrontDesk,Reservations")]
        public async Task<IActionResult> Unblock([FromBody] BlockAvailabilityRequest req)
        {
            await _db.UnblockAvailability(req);
            return Ok(ApiResponse<string>.Ok("Rooms unblocked"));
        }
    }

    // ── CHANNEL PARTNERS CONTROLLER ────────────────────────────────────────────
    [ApiController]
    [Route("api/channels")]
    [Authorize]
    public class ChannelsController : ControllerBase
    {
        private readonly DatabaseService _db;
        private int HotelId =>
            int.TryParse(User.FindFirst("HotelId")?.Value, out var h) ? h : 1;
        public ChannelsController(DatabaseService db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] bool? activeOnly = true)
            => Ok(ApiResponse<object>.Ok(await _db.GetChannelPartners(HotelId, activeOnly)));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var cp = await _db.GetChannelPartner(id);
            return cp == null ? NotFound() : Ok(ApiResponse<ChannelPartner>.Ok(cp));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> Save([FromBody] ChannelPartner cp)
        {
            cp.HotelId = HotelId;
            var id = await _db.SaveChannelPartner(cp);
            return Ok(ApiResponse<int>.Ok(id,
                cp.PartnerId == 0 ? "Channel partner created" : "Updated"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var cp = await _db.GetChannelPartner(id);
            if (cp == null) return NotFound();
            cp.IsActive = false;
            await _db.SaveChannelPartner(cp);
            return Ok(ApiResponse<string>.Ok("Channel partner deactivated"));
        }

        [HttpGet("rate-mappings")]
        public async Task<IActionResult> GetMappings([FromQuery] int? partnerId)
            => Ok(ApiResponse<object>.Ok(await _db.GetChannelRateMappings(partnerId)));

        [HttpPost("rate-mappings")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> SaveMapping([FromBody] ChannelRateMapping m)
        {
            await _db.SaveChannelRateMapping(m);
            return Ok(ApiResponse<string>.Ok("Rate mapping saved"));
        }

        [HttpGet("remittances")]
        public async Task<IActionResult> GetRemittances([FromQuery] int? partnerId)
            => Ok(ApiResponse<object>.Ok(await _db.GetRemittances(partnerId)));

        [HttpPost("remittances")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin,Finance")]
        public async Task<IActionResult> SaveRemittance([FromBody] PartnerRemittance r)
        {
            var id = await _db.SaveRemittance(r);
            return Ok(ApiResponse<int>.Ok(id, "Remittance saved"));
        }
    }

    // ── CUSTOMERS CONTROLLER ───────────────────────────────────────────────────
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly DatabaseService _db;
        public CustomersController(DatabaseService db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int size = 25)
        {
            var (items, total) = await _db.SearchCustomers(q, page, size);
            return Ok(ApiResponse<object>.Ok(items, total: total));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var c = await _db.GetCustomer(id);
            return c == null ? NotFound() : Ok(ApiResponse<Customer>.Ok(c));
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] Customer c)
        {
            var id = await _db.SaveCustomer(c);
            return Ok(ApiResponse<int>.Ok(id, "Customer saved"));
        }
    }

    // ── BOOKING ADD-ON CATALOG CONTROLLER ─────────────────────────────────────
    [ApiController]
    [Route("api/booking-addons")]
    public class BookingAddonsController : ControllerBase
    {
        private readonly DatabaseService _db;
        private int HotelId => int.TryParse(User.FindFirst("HotelId")?.Value, out var h) ? h : 1;
        private int UserId  => int.TryParse(User.FindFirst("UserId")?.Value,  out var u) ? u : 0;

        public BookingAddonsController(DatabaseService db) => _db = db;

        /// <summary>Get all active add-ons for the hotel (used by booking form — public).</summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCatalog([FromQuery] bool all = false)
        {
            var items = await _db.GetAddonCatalog(HotelId, availableOnly: !all);
            return Ok(ApiResponse<object>.Ok(items));
        }

        /// <summary>Get add-on items saved against a booking.</summary>
        [HttpGet("booking/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> GetForBooking(int bookingId)
        {
            var items = await _db.GetBookingAddons(bookingId);
            return Ok(ApiResponse<object>.Ok(items));
        }

        /// <summary>Create a new add-on catalog item (admin only).</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] BookingAddonCatalog item)
        {
            item.HotelId = HotelId;
            var id = await _db.SaveAddonCatalog(item);
            await _db.LogAudit(UserId, "CREATE_ADDON", "BookingAddonCatalog", id.ToString());
            return Ok(ApiResponse<object>.Ok(new { addonId = id }, "Add-on created"));
        }

        /// <summary>Update an existing add-on catalog item (admin only).</summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] BookingAddonCatalog item)
        {
            item.AddonId = id;
            item.HotelId = HotelId;
            await _db.SaveAddonCatalog(item);
            await _db.LogAudit(UserId, "UPDATE_ADDON", "BookingAddonCatalog", id.ToString());
            return Ok(ApiResponse<string>.Ok("Add-on updated"));
        }

        /// <summary>Deactivate an add-on (soft delete).</summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            await _db.DeactivateAddon(id);
            await _db.LogAudit(UserId, "DEACTIVATE_ADDON", "BookingAddonCatalog", id.ToString());
            return Ok(ApiResponse<string>.Ok("Add-on deactivated"));
        }
    }

    // ── BOOKINGS CONTROLLER ────────────────────────────────────────────────────
    [ApiController]
    [Route("api/bookings")]
    public class BookingsController : ControllerBase
    {
        private readonly DatabaseService _db;

        // FIX: Safe UserId — returns 0 for anonymous website requests, never throws
        private int UserId
        {
            get
            {
                var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(v, out var id) ? id : 0;
            }
        }

        public BookingsController(DatabaseService db) => _db = db;

        [HttpPost("quote")]
        [AllowAnonymous]
        public async Task<IActionResult> GetQuote([FromBody] PriceQuoteRequest req)
        {
            if (req.CheckOutDate <= req.CheckInDate)
                return BadRequest(ApiResponse<string>.Fail("Check-out must be after check-in"));
            if (req.CheckInDate.Date < DateTime.UtcNow.Date)
                return BadRequest(ApiResponse<string>.Fail("Check-in cannot be in the past"));
            var quote = await _db.GetPriceQuote(req);
            return Ok(ApiResponse<PriceQuoteResponse>.Ok(quote));
        }

        // FIX: AllowAnonymous so public hotel website can create bookings without JWT
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (req.CheckInDate.Date < DateTime.UtcNow.Date)
                return BadRequest(ApiResponse<string>.Fail("Check-in date cannot be in the past"));
            if (req.CheckOutDate.Date <= req.CheckInDate.Date)
                return BadRequest(ApiResponse<string>.Fail("Check-out must be after check-in"));
            var (bookingId, bookingRef, msg) = await _db.CreateBooking(req);
            if (bookingId == 0)
                return BadRequest(ApiResponse<string>.Fail(msg.Replace("ERROR: ", "")));
            if (UserId > 0)
                await _db.LogAudit(UserId, "CREATE_BOOKING", "Bookings", bookingRef);
            var booking = await _db.GetBooking(bookingId);
            return Ok(ApiResponse<Booking>.Ok(booking!, $"Booking created: {bookingRef}"));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> List(
            [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
            [FromQuery] int? partnerId, [FromQuery] string? status,
            [FromQuery] string? bookingSource,
            [FromQuery] int page = 1, [FromQuery] int size = 50)
        {
            var filter = new ReportFilter
            {
                FromDate      = fromDate      ?? DateTime.Today.AddMonths(-3),
                ToDate        = toDate        ?? DateTime.Today.AddDays(90),
                PartnerId     = partnerId,
                Status        = status,
                BookingSource = bookingSource,
                Page          = page,
                PageSize      = size
            };
            var (items, total) = await _db.GetBookings(filter);
            return Ok(ApiResponse<object>.Ok(items, total: total));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> Get(int id)
        {
            var b = await _db.GetBooking(id);
            return b == null
                ? NotFound(ApiResponse<string>.Fail("Booking not found"))
                : Ok(ApiResponse<Booking>.Ok(b));
        }

        // FIX: AllowAnonymous so guests can track their own booking by reference
        [HttpGet("reference/{ref}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByRef(string @ref)
        {
            var b = await _db.GetBookingByRef(@ref.ToUpper());
            return b == null
                ? NotFound(ApiResponse<string>.Fail("Booking not found"))
                : Ok(ApiResponse<Booking>.Ok(b));
        }

        // FIX: AllowAnonymous so guests can cancel via the public tracking page
        [HttpPost("{id}/cancel")]
        [AllowAnonymous]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelBookingRequest req)
        {
            var (msg, charge) = await _db.CancelBooking(id, req.Reason, UserId);
            if (msg.StartsWith("ERROR"))
                return BadRequest(ApiResponse<string>.Fail(msg.Replace("ERROR: ", "")));
            if (UserId > 0)
                await _db.LogAudit(UserId, "CANCEL_BOOKING", "Bookings",
                    id.ToString(), req.Reason);
            return Ok(ApiResponse<object>.Ok(
                new { message = msg, cancellationCharge = charge }));
        }

        [HttpPost("{id}/checkin")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin,FrontDesk")]
        public async Task<IActionResult> CheckIn(int id, [FromQuery] int? roomId)
        {
            var b = await _db.GetBooking(id);
            if (b == null) return NotFound(ApiResponse<string>.Fail("Booking not found"));
            if (b.BookingStatus != "Confirmed")
                return BadRequest(ApiResponse<string>.Fail(
                    $"Cannot check in — status is {b.BookingStatus}"));
            await _db.UpdateBookingStatus(id, "CheckedIn", roomId);
            await _db.LogAudit(UserId, "CHECKIN", "Bookings", id.ToString());
            return Ok(ApiResponse<string>.Ok("Guest checked in successfully"));
        }

        [HttpPost("{id}/checkout")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin,FrontDesk")]
        public async Task<IActionResult> CheckOut(int id)
        {
            var b = await _db.GetBooking(id);
            if (b == null) return NotFound(ApiResponse<string>.Fail("Booking not found"));
            if (b.BookingStatus != "CheckedIn")
                return BadRequest(ApiResponse<string>.Fail("Guest is not checked in"));
            await _db.UpdateBookingStatus(id, "CheckedOut");
            await _db.LogAudit(UserId, "CHECKOUT", "Bookings", id.ToString());
            return Ok(ApiResponse<string>.Ok("Guest checked out successfully"));
        }

        [HttpPost("{id}/notes")]
        [Authorize]
        public async Task<IActionResult> UpdateNotes(int id, [FromBody] dynamic body)
        {
            string? notes = body?.notes?.ToString();
            await _db.UpdateBookingNotes(id, notes);
            return Ok(ApiResponse<string>.Ok("Notes updated"));
        }
    }

    // ── PAYMENTS CONTROLLER ────────────────────────────────────────────────────
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly DatabaseService _db;
        private int UserId
        {
            get
            {
                var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(v, out var id) ? id : 0;
            }
        }
        public PaymentsController(DatabaseService db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] int? bookingId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
            => Ok(ApiResponse<object>.Ok(await _db.GetPayments(bookingId, from, to)));

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HotelAdmin,FrontDesk,Finance")]
        public async Task<IActionResult> Record([FromBody] RecordPaymentRequest req)
        {
            var booking = await _db.GetBooking(req.BookingId);
            if (booking == null) return NotFound(ApiResponse<string>.Fail("Booking not found"));
            var pmt = new Payment
            {
                BookingId      = req.BookingId,
                Amount         = req.Amount,
                PaymentType    = req.PaymentType,
                PaymentMethod  = req.PaymentMethod,
                TransactionRef = req.TransactionRef,
                GatewayName    = req.GatewayName,
                GatewayTxnId   = req.GatewayTxnId,
                Status         = "Completed",
                Notes          = req.Notes,
                ProcessedBy    = UserId
            };
            var id = await _db.RecordPayment(pmt);
            await _db.LogAudit(UserId, "RECORD_PAYMENT", "Payments",
                id.ToString(), $"₹{req.Amount} via {req.PaymentMethod}");
            return Ok(ApiResponse<int>.Ok(id, $"Payment of ₹{req.Amount} recorded"));
        }
    }

    // ── REPORTS CONTROLLER ─────────────────────────────────────────────────────
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly DatabaseService _db;
        public ReportsController(DatabaseService db) => _db = db;

        [HttpGet("dashboard/{hotelId}")]
        public async Task<IActionResult> Dashboard(int hotelId)
            => Ok(ApiResponse<DashboardStats>.Ok(await _db.GetDashboardStats(hotelId)));

        [HttpGet("bookings")]
        public async Task<IActionResult> BookingsReport(
            [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
            [FromQuery] int? partnerId, [FromQuery] string? status,
            [FromQuery] string? bookingSource,
            [FromQuery] int page = 1, [FromQuery] int size = 100)
        {
            var filter = new ReportFilter
            {
                FromDate      = fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                ToDate        = toDate   ?? DateTime.Today,
                PartnerId     = partnerId,
                Status        = status,
                BookingSource = bookingSource,
                Page          = page,
                PageSize      = size
            };
            var (items, total) = await _db.GetBookings(filter);
            return Ok(ApiResponse<object>.Ok(items, total: total));
        }

        [HttpGet("channels")]
        public async Task<IActionResult> ChannelSummary()
        {
            var stats = await _db.GetDashboardStats(1);
            return Ok(ApiResponse<object>.Ok(stats.ChannelSummary));
        }

        [HttpGet("occupancy")]
        public async Task<IActionResult> Occupancy(
            [FromQuery] int? roomTypeId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var avail = await _db.GetAvailability(
                roomTypeId,
                from ?? DateTime.Today.AddDays(-7),
                to   ?? DateTime.Today.AddDays(30));
            return Ok(ApiResponse<object>.Ok(avail));
        }

        [HttpGet("payments")]
        public async Task<IActionResult> PaymentsReport(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
            => Ok(ApiResponse<object>.Ok(await _db.GetPayments(null, from, to)));

        [HttpGet("remittances")]
        public async Task<IActionResult> RemittancesReport([FromQuery] int? partnerId)
            => Ok(ApiResponse<object>.Ok(await _db.GetRemittances(partnerId)));

        [HttpGet("audit")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> AuditLogs([FromQuery] int page = 1)
            => Ok(ApiResponse<object>.Ok(await _db.GetAuditLogs(page)));
    }

    // ── USERS CONTROLLER ───────────────────────────────────────────────────────
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "SuperAdmin,HotelAdmin")]
    public class UsersController : ControllerBase
    {
        private readonly DatabaseService _db;
        private int HotelId =>
            int.TryParse(User.FindFirst("HotelId")?.Value, out var h) ? h : 1;
        public UsersController(DatabaseService db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> List()
            => Ok(ApiResponse<object>.Ok(await _db.GetUsers(HotelId)));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = new User
            {
                HotelId      = req.HotelId ?? HotelId,
                Username     = req.Username,
                PasswordHash = AuthService.HashPassword(req.Password),
                FullName     = req.FullName,
                Email        = req.Email,
                Phone        = req.Phone,
                Role         = req.Role,
                IsActive     = true
            };
            var id = await _db.CreateUser(user);
            return Ok(ApiResponse<int>.Ok(id, "User created"));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
        {
            await _db.UpdateUser(
                id, req.FullName, req.Email, req.Phone,
                req.Role, req.IsActive, req.HotelId);
            return Ok(ApiResponse<string>.Ok("User updated"));
        }

        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] dynamic body)
        {
            string pass = body?.password?.ToString() ?? "Hotel@2024";
            await _db.UpdateUserPassword(id, AuthService.HashPassword(pass));
            return Ok(ApiResponse<string>.Ok("Password reset"));
        }
    }

    // ── CUSTOMER PORTAL CONTROLLER ─────────────────────────────────────────────
    // Provides guest-facing auth (login via email + booking ref) and
    // a "my bookings" endpoint that returns all bookings for that email.
    [ApiController]
    [Route("api/customers")]
    public class CustomerPortalController : ControllerBase
    {
        private readonly DatabaseService _db;
        private readonly AuthService     _auth;
        public CustomerPortalController(DatabaseService db, AuthService auth)
        {
            _db   = db;
            _auth = auth;
        }

        // POST /api/customers/login
        // Body: { "email": "guest@example.com", "bookingReference": "BK20260309123456" }
        // Verifies that a booking with that reference exists AND belongs to that email.
        // Returns a 24-hour customer JWT on success.
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> CustomerLogin([FromBody] CustomerLoginRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail("Email and booking reference are required"));

            var booking = await _db.GetBookingByRef(req.BookingReference.Trim().ToUpper());
            if (booking == null)
                return Unauthorized(ApiResponse<string>.Fail(
                    "Booking reference not found. Please check and try again.", "NOT_FOUND"));

            if (!string.Equals(booking.GuestEmail, req.Email.Trim(),
                StringComparison.OrdinalIgnoreCase))
                return Unauthorized(ApiResponse<string>.Fail(
                    "Email address does not match this booking.", "EMAIL_MISMATCH"));

            var token = _auth.GenerateCustomerToken(
                req.Email.Trim().ToLower(), booking.GuestName ?? req.Email);

            var customer = await _db.FindCustomerByEmail(req.Email.Trim());

            return Ok(ApiResponse<CustomerLoginResponse>.Ok(new CustomerLoginResponse
            {
                Token      = token,
                GuestName  = booking.GuestName ?? req.Email,
                Email      = req.Email.Trim().ToLower(),
                TotalStays = customer?.TotalStays ?? 1,
                ExpiresAt  = DateTime.UtcNow.AddHours(24)
            }, "Welcome back!"));
        }

        // GET /api/customers/my-bookings
        // Requires customer JWT. Returns all bookings associated with the token email.
        [HttpGet("my-bookings")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyBookings()
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                     ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized(ApiResponse<string>.Fail("Invalid token"));

            var bookings = await _db.GetBookingsByEmail(email);
            return Ok(ApiResponse<object>.Ok(bookings));
        }
    }

    // ── SETTINGS CONTROLLER ────────────────────────────────────────────────────
    [ApiController]
    [Route("api/settings")]
    [Authorize(Roles = "SuperAdmin,HotelAdmin")]
    public class SettingsController : ControllerBase
    {
        private readonly DatabaseService _db;
        public SettingsController(DatabaseService db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get()
            => Ok(ApiResponse<object>.Ok(await _db.GetSettings()));

        [HttpPost("{key}")]
        public async Task<IActionResult> Save(string key, [FromBody] dynamic body)
        {
            await _db.SaveSetting(key, body?.value?.ToString());
            return Ok(ApiResponse<string>.Ok("Setting saved"));
        }
    }
    // ── ORDERS CONTROLLER (OMS) ────────────────────────────────────────────────
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly DatabaseService _db;

        private int UserId
        {
            get
            {
                var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(v, out var id) ? id : 0;
            }
        }

        private int HotelId =>
            int.TryParse(User.FindFirst("HotelId")?.Value, out var h) ? h : 1;

        public OrdersController(DatabaseService db) => _db = db;

        // ── CATALOG ───────────────────────────────────────────────────────────

        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalog([FromQuery] string? category)
        {
            var items = await _db.GetOrderCatalog(HotelId, category);
            return Ok(ApiResponse<object>.Ok(items));
        }

        [HttpGet("catalog/all")]
        public async Task<IActionResult> GetAllCatalog()
        {
            var items = await _db.GetAllOrderCatalog(HotelId);
            return Ok(ApiResponse<object>.Ok(items));
        }

        [HttpPost("catalog")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> CreateCatalogItem([FromBody] OrderCatalogItem item)
        {
            item.HotelId = HotelId;
            var id = await _db.CreateCatalogItem(item);
            await _db.LogAudit(UserId, "CREATE_CATALOG_ITEM", "OrderCatalog", id.ToString());
            return Ok(ApiResponse<int>.Ok(id, "Catalog item created"));
        }

        [HttpPut("catalog/{id}")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> UpdateCatalogItem(int id, [FromBody] OrderCatalogItem item)
        {
            item.CatalogId = id;
            await _db.UpdateCatalogItem(item);
            await _db.LogAudit(UserId, "UPDATE_CATALOG_ITEM", "OrderCatalog", id.ToString());
            return Ok(ApiResponse<string>.Ok("Catalog item updated"));
        }

        [HttpDelete("catalog/{id}")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> DeleteCatalogItem(int id)
        {
            await _db.DeleteCatalogItem(id);
            await _db.LogAudit(UserId, "DELETE_CATALOG_ITEM", "OrderCatalog", id.ToString());
            return Ok(ApiResponse<string>.Ok("Catalog item removed"));
        }

        // ── ORDERS ────────────────────────────────────────────────────────────

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var stats = await _db.GetOrderStats(HotelId, from, to);
            return Ok(ApiResponse<OrderSummaryStats>.Ok(stats));
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders(
            [FromQuery] string? status,
            [FromQuery] string? category,
            [FromQuery] string? orderType,
            [FromQuery] int? bookingId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var orders = await _db.GetOrders(HotelId, status, category, orderType, bookingId, from, to, page, pageSize);
            return Ok(ApiResponse<object>.Ok(orders));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _db.GetOrderById(id);
            if (order == null) return NotFound(ApiResponse<string>.Fail("Order not found"));
            var items   = await _db.GetOrderItems(id);
            var history = await _db.GetOrderHistory(id);
            return Ok(ApiResponse<object>.Ok(new { order, items, history }));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail("Invalid request data"));

            // Validate by order type
            if (req.OrderType == "InRoom" && (req.BookingId == null || req.BookingId == 0))
                return BadRequest(ApiResponse<string>.Fail("InRoom order requires a Booking"));
            if (req.OrderType == "DirectSale" && string.IsNullOrWhiteSpace(req.WalkInGuestName))
                return BadRequest(ApiResponse<string>.Fail("Guest name is required for Direct Sale orders"));

            try
            {
                var (orderId, orderNumber, msg) = await _db.CreateOrder(
                    HotelId,
                    req.OrderType,
                    req.BookingId, req.RoomId, req.CustomerId,
                    req.WalkInGuestName, req.WalkInGuestPhone,
                    req.Category, req.Priority ?? "Normal",
                    req.SpecialInstructions, req.DeliveryTime, UserId);

                if (!msg.StartsWith("SUCCESS"))
                    return BadRequest(ApiResponse<string>.Fail(msg.Replace("ERROR: ", "")));

                foreach (var item in req.Items)
                    await _db.AddOrderItem(orderId, item);

                await _db.LogAudit(UserId, "CREATE_ORDER", "Orders", orderId.ToString(),
                    $"Type: {req.OrderType}, Category: {req.Category}");
                return Ok(ApiResponse<object>.Ok(
                    new { orderId, orderNumber }, msg.Replace("SUCCESS: ", "")));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Fail($"Error: {ex.Message}"));
            }
        }

        [HttpPost("{id}/items")]
        public async Task<IActionResult> AddItem(int id, [FromBody] OrderItemRequest item)
        {
            await _db.AddOrderItem(id, item);
            return Ok(ApiResponse<string>.Ok("Item added"));
        }

        [HttpDelete("{id}/items/{itemId}")]
        public async Task<IActionResult> RemoveItem(int id, int itemId)
        {
            await _db.RemoveOrderItem(itemId);
            return Ok(ApiResponse<string>.Ok("Item removed"));
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest req)
        {
            var msg = await _db.UpdateOrderStatus(id, req.Status, UserId, req.Notes);
            if (msg.StartsWith("ERROR"))
                return BadRequest(ApiResponse<string>.Fail(msg.Replace("ERROR: ", "")));
            await _db.LogAudit(UserId, $"ORDER_STATUS_{req.Status.ToUpper()}", "Orders", id.ToString());
            // WARNING = status updated but inventory deduction had a shortage — surface to frontend
            bool hasWarning = msg.StartsWith("WARNING");
            string cleanMsg = msg.Replace("SUCCESS: ", "").Replace("WARNING: ", "");
            // Prefix message with INVENTORY_WARNING so frontend can detect and show an alert
            string responseMsg = hasWarning ? $"INVENTORY_WARNING: {cleanMsg}" : cleanMsg;
            return Ok(ApiResponse<string>.Ok(responseMsg));
        }

        [HttpPost("{id}/bill")]
        public async Task<IActionResult> BillOrder(int id)
        {
            var (billId, receiptNo, msg) = await _db.BillOrder(id, UserId);
            if (msg.StartsWith("ERROR"))
                return BadRequest(ApiResponse<string>.Fail(msg.Replace("ERROR: ", "")));
            await _db.LogAudit(UserId, "BILL_ORDER", "Orders", id.ToString(),
                $"BillEntry #{billId}");
            return Ok(ApiResponse<object>.Ok(new { billEntryId = billId, receiptNo },
                msg.Replace("SUCCESS: ", "")));
        }

        /// <summary>
        /// Quick-bill for DirectSale orders — advances to Delivered + Billed in one step.
        /// Ideal for standalone restaurant use where the full status flow is too slow.
        /// </summary>
        [HttpPost("{id}/quickbill")]
        public async Task<IActionResult> QuickBillOrder(int id)
        {
            var (billId, receiptNo, msg) = await _db.QuickBillDirectSale(id, UserId);
            if (msg.StartsWith("ERROR"))
                return BadRequest(ApiResponse<string>.Fail(msg.Replace("ERROR: ", "")));
            await _db.LogAudit(UserId, "QUICKBILL_ORDER", "Orders", id.ToString(),
                $"DirectSale receipt: {receiptNo}");
            return Ok(ApiResponse<object>.Ok(
                new { billEntryId = billId, receiptNo },
                msg.Replace("SUCCESS: ", "")));
        }

        // ── OMS REPORTS ───────────────────────────────────────────────────────

        [HttpGet("report")]
        public async Task<IActionResult> GetOrderReport(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? orderType,
            [FromQuery] string? category,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            var f = from ?? DateTime.Today.AddMonths(-1);
            var t = to   ?? DateTime.Today;
            var (items, total) = await _db.GetOrderReport(HotelId, f, t, orderType, category, status, page, pageSize);
            return Ok(ApiResponse<object>.Ok(items, total: total));
        }

        [HttpGet("report/by-category")]
        public async Task<IActionResult> GetRevenueByCategory(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? orderType)
        {
            var f = from ?? DateTime.Today.AddMonths(-1);
            var t = to   ?? DateTime.Today;
            var data = await _db.GetOrderRevenueByCategory(HotelId, f, t, orderType);
            return Ok(ApiResponse<object>.Ok(data));
        }

        [HttpGet("report/by-day")]
        public async Task<IActionResult> GetRevenueByDay(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? orderType)
        {
            var f = from ?? DateTime.Today.AddMonths(-1);
            var t = to   ?? DateTime.Today;
            var data = await _db.GetOrderRevenueByDay(HotelId, f, t, orderType);
            return Ok(ApiResponse<object>.Ok(data));
        }

        [HttpGet("report/top-items")]
        public async Task<IActionResult> GetTopItems(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? category,
            [FromQuery] int topN = 10)
        {
            var f = from ?? DateTime.Today.AddMonths(-1);
            var t = to   ?? DateTime.Today;
            var data = await _db.GetTopOrderItems(HotelId, f, t, category, topN);
            return Ok(ApiResponse<object>.Ok(data));
        }

        // ── FOLIO ─────────────────────────────────────────────────────────────

        [HttpGet("folio/{bookingId}")]
        public async Task<IActionResult> GetFolio(int bookingId)
        {
            var entries = await _db.GetBookingFolio(bookingId);
            var invoice = await _db.GetCheckoutInvoice(bookingId);
            return Ok(ApiResponse<object>.Ok(new { entries, invoice }));
        }

        [HttpPost("folio/manual")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin,FrontDesk")]
        public async Task<IActionResult> PostManualCharge([FromBody] ManualBillEntryRequest req)
        {
            var entry = new BillEntry
            {
                BookingId   = req.BookingId,
                EntryType   = req.EntryType,
                Description = req.Description,
                Amount      = req.Amount,
                TaxAmount   = req.TaxAmount,
                GrandAmount = req.Amount + req.TaxAmount,
                PostedBy    = UserId
            };
            var id = await _db.PostManualBillEntry(entry);
            await _db.LogAudit(UserId, "POST_MANUAL_CHARGE", "BillEntries", id.ToString());
            return Ok(ApiResponse<int>.Ok(id, "Charge posted to folio"));
        }

        [HttpPost("folio/{entryId}/void")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> VoidEntry(int entryId, [FromBody] VoidEntryRequest req)
        {
            await _db.VoidBillEntry(entryId, UserId, req.Reason ?? "");
            await _db.LogAudit(UserId, "VOID_BILL_ENTRY", "BillEntries", entryId.ToString());
            return Ok(ApiResponse<string>.Ok("Entry voided"));
        }

        // ── INVOICE ───────────────────────────────────────────────────────────

        [HttpPost("invoice/generate/{bookingId}")]
        public async Task<IActionResult> GenerateInvoice(int bookingId)
        {
            var (invoiceId, msg) = await _db.GenerateCheckoutInvoice(bookingId, UserId);
            if (msg.StartsWith("ERROR"))
                return BadRequest(ApiResponse<string>.Fail(msg.Replace("ERROR: ", "")));
            await _db.LogAudit(UserId, "GENERATE_INVOICE", "CheckoutInvoices", invoiceId.ToString());
            return Ok(ApiResponse<object>.Ok(new { invoiceId },
                msg.Replace("SUCCESS: ", "").Replace("INFO: ", "")));
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // RECIPE-BASED INVENTORY MANAGEMENT SYSTEM (RIMS)
    // ══════════════════════════════════════════════════════════════════════════

    // ── /api/inventory ────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/inventory")]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly DatabaseService _db;
        private int HotelId => int.TryParse(User.FindFirst("HotelId")?.Value, out var h) ? h : 1;
        private int UserId  => int.TryParse(User.FindFirst("UserId")?.Value,  out var u) ? u : 0;

        public InventoryController(DatabaseService db) => _db = db;

        // ── Dashboard ────────────────────────────────────────────────────────
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var stats = await _db.GetInventoryDashboard(HotelId);
            return Ok(ApiResponse<InventoryDashboardStats>.Ok(stats));
        }

        // ── Inventory Items ──────────────────────────────────────────────────
        [HttpGet("items")]
        public async Task<IActionResult> GetItems([FromQuery] string? category, [FromQuery] bool lowStockOnly = false)
        {
            var items = await _db.GetInventoryItems(HotelId, category, lowStockOnly);
            return Ok(ApiResponse<IEnumerable<InventoryItem>>.Ok(items));
        }

        [HttpGet("items/{id}")]
        public async Task<IActionResult> GetItem(int id)
        {
            var item = await _db.GetInventoryItemById(id);
            if (item == null) return NotFound(ApiResponse<string>.Fail("Item not found"));
            return Ok(ApiResponse<InventoryItem>.Ok(item));
        }

        [HttpPost("items")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> CreateItem([FromBody] CreateInventoryItemRequest req)
        {
            var id = await _db.CreateInventoryItem(HotelId, req);
            await _db.LogAudit(UserId, "CREATE_INV_ITEM", "InventoryItems", id.ToString());
            return Ok(ApiResponse<int>.Ok(id, "Inventory item created"));
        }

        [HttpPut("items/{id}")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] CreateInventoryItemRequest req)
        {
            await _db.UpdateInventoryItem(id, req);
            await _db.LogAudit(UserId, "UPDATE_INV_ITEM", "InventoryItems", id.ToString());
            return Ok(ApiResponse<string>.Ok("Item updated"));
        }

        [HttpDelete("items/{id}")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            await _db.DeactivateInventoryItem(id);
            await _db.LogAudit(UserId, "DELETE_INV_ITEM", "InventoryItems", id.ToString());
            return Ok(ApiResponse<string>.Ok("Item deactivated"));
        }

        // ── Stock Movements ──────────────────────────────────────────────────
        [HttpGet("movements")]
        public async Task<IActionResult> GetMovements(
            [FromQuery] int? itemId,
            [FromQuery] string? movementType,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var rows = await _db.GetStockMovements(HotelId, itemId, movementType, from, to, page, pageSize);
            return Ok(ApiResponse<IEnumerable<StockMovement>>.Ok(rows));
        }

        [HttpPost("movements")]
        public async Task<IActionResult> AddMovement([FromBody] StockMovementRequest req)
        {
            var (movId, msg) = await _db.AddStockMovement(HotelId, UserId, req);
            if (msg.StartsWith("ERROR")) return BadRequest(ApiResponse<string>.Fail(msg.Replace("ERROR: ", "")));
            await _db.LogAudit(UserId, $"STOCK_{req.MovementType}", "StockMovements", movId.ToString(),
                $"Item:{req.ItemId} Qty:{req.Quantity}");
            return Ok(ApiResponse<int>.Ok(movId, msg.Replace("SUCCESS: ", "")));
        }
    }

    // ── /api/recipes ──────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/recipes")]
    [Authorize]
    public class RecipesController : ControllerBase
    {
        private readonly DatabaseService _db;
        private int HotelId => int.TryParse(User.FindFirst("HotelId")?.Value, out var h) ? h : 1;
        private int UserId  => int.TryParse(User.FindFirst("UserId")?.Value,  out var u) ? u : 0;

        public RecipesController(DatabaseService db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetRecipes([FromQuery] string? category)
        {
            var recipes = await _db.GetRecipes(HotelId, category);
            return Ok(ApiResponse<IEnumerable<Recipe>>.Ok(recipes));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecipe(int id)
        {
            var recipe = await _db.GetRecipeById(id);
            if (recipe == null) return NotFound(ApiResponse<string>.Fail("Recipe not found"));
            return Ok(ApiResponse<Recipe>.Ok(recipe));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> CreateRecipe([FromBody] CreateRecipeRequest req)
        {
            var id = await _db.CreateRecipe(HotelId, req);
            await _db.LogAudit(UserId, "CREATE_RECIPE", "Recipes", id.ToString());
            return Ok(ApiResponse<int>.Ok(id, "Recipe created"));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> UpdateRecipe(int id, [FromBody] CreateRecipeRequest req)
        {
            await _db.UpdateRecipe(id, req);
            await _db.LogAudit(UserId, "UPDATE_RECIPE", "Recipes", id.ToString());
            return Ok(ApiResponse<string>.Ok("Recipe updated"));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,HotelAdmin")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            await _db.DeactivateRecipe(id);
            await _db.LogAudit(UserId, "DELETE_RECIPE", "Recipes", id.ToString());
            return Ok(ApiResponse<string>.Ok("Recipe deactivated"));
        }

        /// <summary>
        /// Deduct ingredients from stock for N portions of a recipe.
        /// Called automatically when an order is marked Delivered, or manually.
        /// </summary>
        [HttpPost("deduct-stock")]
        public async Task<IActionResult> DeductStock([FromBody] DeductRecipeStockRequest req)
        {
            var (ok, msg) = await _db.DeductRecipeStock(HotelId, UserId, req);
            if (!ok) return BadRequest(ApiResponse<string>.Fail(msg));
            await _db.LogAudit(UserId, "RECIPE_DEDUCT", "Recipes", req.RecipeId.ToString(),
                $"Portions:{req.Portions} Order:{req.OrderId}");
            return Ok(ApiResponse<string>.Ok(msg));
        }

        /// <summary>Check stock for N portions — returns MaxPortionsPossible so UI can show "you can make X more".</summary>
        [HttpGet("{id}/stock-check")]
        public async Task<IActionResult> StockCheck(int id, [FromQuery] int portions = 1)
        {
            var result = await _db.CheckRecipeStock(id, portions);
            return Ok(ApiResponse<RecipeStockCheckResult>.Ok(result));
        }
    }
}
