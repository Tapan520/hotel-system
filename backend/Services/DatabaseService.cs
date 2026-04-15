using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using HotelChannelManager.Models;
using HotelChannelManager.DTOs;

namespace HotelChannelManager.Services
{
    // ══════════════════════════════════════════════════════════════════════════
    // BUG FIX — MySqlDateTimeTypeHandler
    //
    // ROOT CAUSE of "Error parsing column 9 (LastLoginAt=3/3/2026 5:28:31 PM - Object)":
    //
    //   1. First login:  LastLoginAt is NULL → Dapper maps NULL → DateTime? = null  ✅
    //   2. UpdateLastLogin() sets LastLoginAt = NOW()
    //   3. Second login: SELECT * FROM users returns LastLoginAt as MySqlDateTime object
    //   4. Dapper tries to cast MySqlDateTime → DateTime? and throws "Object" mismatch
    //
    // FIX A: Register MySqlDateTimeTypeHandler globally (handles all queries)
    // FIX B: Use CAST(LastLoginAt AS CHAR) in GetUserByUsername so MySQL returns
    //         a string that the nullable handler parses cleanly — belt and braces.
    // ══════════════════════════════════════════════════════════════════════════

    public class MySqlDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.DbType = DbType.DateTime;
            parameter.Value = value;
        }

        public override DateTime Parse(object value)
        {
            if (value == null || value == DBNull.Value) return DateTime.MinValue;
            if (value is MySql.Data.Types.MySqlDateTime mdt)
                return mdt.IsValidDateTime ? mdt.GetDateTime() : DateTime.MinValue;
            if (value is string s && DateTime.TryParse(s, out var p)) return p;
            if (value is DateTime dt) return dt;
            try { return Convert.ToDateTime(value); } catch { return DateTime.MinValue; }
        }
    }

    public class MySqlNullableDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime?>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime? value)
        {
            parameter.DbType = DbType.DateTime;
            parameter.Value = (object?)value ?? DBNull.Value;
        }

        public override DateTime? Parse(object value)
        {
            if (value == null || value == DBNull.Value) return null;
            if (value is MySql.Data.Types.MySqlDateTime mdt)
                return mdt.IsValidDateTime ? mdt.GetDateTime() : (DateTime?)null;
            if (value is string s) return DateTime.TryParse(s, out var p) ? p : (DateTime?)null;
            if (value is DateTime dt) return dt;
            try { return Convert.ToDateTime(value); } catch { return null; }
        }
    }

    public class DatabaseService
    {
        private readonly string _conn;

        static DatabaseService()
        {
            SqlMapper.RemoveTypeMap(typeof(DateTime));
            SqlMapper.RemoveTypeMap(typeof(DateTime?));
            SqlMapper.AddTypeHandler(new MySqlDateTimeTypeHandler());
            SqlMapper.AddTypeHandler(new MySqlNullableDateTimeTypeHandler());
        }

        public DatabaseService(IConfiguration config)
        {
            // ── Railway connection ────────────────────────────────────────────
            // Railway MYSQL_PUBLIC_URL ends with /railway (Railway default DB name)
            // BUT our schema lives in database 'hotelchannelmanager'.
            // We parse the URL for host/port/credentials, then override the DB name.
            // Falls back to appsettings.json DefaultConnection for local dev.
            var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_PUBLIC_URL")
                        ?? Environment.GetEnvironmentVariable("DATABASE_URL");

            if (!string.IsNullOrEmpty(mysqlUrl))
            {
                // HOTEL_DB_NAME can override; default is hotelchannelmanager
                var dbName = Environment.GetEnvironmentVariable("HOTEL_DB_NAME")
                          ?? "hotelchannelmanager";
                _conn = BuildConnectionStringFromUrl(mysqlUrl, dbName);
            }
            else
            {
                _conn = config.GetConnectionString("DefaultConnection")!;
            }
        }

        /// <summary>
        /// Parses  mysql://USER:PASSWORD@HOST:PORT/anything
        /// into a MySql.Data connection string, using dbOverride as the database name.
        /// </summary>
        private static string BuildConnectionStringFromUrl(string url, string dbOverride)
        {
            var uri      = new Uri(url);
            var host     = uri.Host;
            var port     = uri.Port > 0 ? uri.Port : 3306;
            var userInfo = uri.UserInfo.Split(':', 2);
            var user     = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";

            // Use dbOverride — NOT uri.AbsolutePath — so we always hit hotelchannelmanager
            return $"Server={host};Port={port};Database={dbOverride};" +
                   $"User={user};Password={password};" +
                   "SslMode=Preferred;AllowPublicKeyRetrieval=True;CharSet=utf8mb4;" +
                   "ConnectionTimeout=30;DefaultCommandTimeout=60;" +
                   "ConvertZeroDateTime=True;AllowZeroDateTime=True;TreatTinyAsBoolean=True;";
        }

        private IDbConnection GetDb() => new MySqlConnection(_conn);

        public async Task<bool> TestConnection()
        {
            try
            {
                using var db = GetDb();
                var result = await db.ExecuteScalarAsync<int>("SELECT 1");
                return result == 1;
            }
            catch { return false; }
        }

        // ── HOTELS ─────────────────────────────────────────────────────────
        public async Task<IEnumerable<Hotel>> GetHotels()
        {
            using var db = GetDb();
            return await db.QueryAsync<Hotel>("SELECT * FROM hotels WHERE IsActive=1 ORDER BY HotelName");
        }

        public async Task<Hotel?> GetHotel(int id)
        {
            using var db = GetDb();
            return await db.QueryFirstOrDefaultAsync<Hotel>("SELECT * FROM hotels WHERE HotelId=@Id", new { Id = id });
        }

        public async Task<int> SaveHotel(Hotel h)
        {
            using var db = GetDb();
            if (h.HotelId == 0)
                return await db.ExecuteScalarAsync<int>(
                    @"INSERT INTO hotels(HotelName,Address,City,State,Country,ZipCode,Phone,Email,
                      Website,StarRating,CheckInTime,CheckOutTime,CancellationPolicyHours,
                      LateCancelChargePercent,TaxPercent,CurrencyCode,Description)
                      VALUES(@HotelName,@Address,@City,@State,@Country,@ZipCode,@Phone,@Email,
                      @Website,@StarRating,@CheckInTime,@CheckOutTime,@CancellationPolicyHours,
                      @LateCancelChargePercent,@TaxPercent,@CurrencyCode,@Description);
                      SELECT LAST_INSERT_ID();", h);
            await db.ExecuteAsync(
                @"UPDATE hotels SET HotelName=@HotelName,Address=@Address,City=@City,State=@State,
                  Country=@Country,ZipCode=@ZipCode,Phone=@Phone,Email=@Email,Website=@Website,
                  StarRating=@StarRating,CheckInTime=@CheckInTime,CheckOutTime=@CheckOutTime,
                  CancellationPolicyHours=@CancellationPolicyHours,
                  LateCancelChargePercent=@LateCancelChargePercent,TaxPercent=@TaxPercent,
                  CurrencyCode=@CurrencyCode,Description=@Description WHERE HotelId=@HotelId", h);
            return h.HotelId;
        }

        // ── ROOM TYPES ─────────────────────────────────────────────────────
        public async Task<IEnumerable<RoomType>> GetRoomTypes(int? hotelId = null)
        {
            using var db = GetDb();
            var sql = "SELECT * FROM roomtypes WHERE IsActive=1" +
                      (hotelId.HasValue ? " AND HotelId=@HotelId" : "") + " ORDER BY SortOrder,TypeName";
            return await db.QueryAsync<RoomType>(sql, new { HotelId = hotelId });
        }

        public async Task<RoomType?> GetRoomType(int id)
        {
            using var db = GetDb();
            return await db.QueryFirstOrDefaultAsync<RoomType>("SELECT * FROM roomtypes WHERE RoomTypeId=@Id", new { Id = id });
        }

        public async Task<int> SaveRoomType(RoomType rt)
        {
            using var db = GetDb();
            if (rt.RoomTypeId == 0)
                return await db.ExecuteScalarAsync<int>(
                    @"INSERT INTO roomtypes(HotelId,TypeName,Description,MaxOccupancy,BedType,SizeInSqFt,ViewType,Amenities,SortOrder)
                      VALUES(@HotelId,@TypeName,@Description,@MaxOccupancy,@BedType,@SizeInSqFt,@ViewType,@Amenities,@SortOrder);
                      SELECT LAST_INSERT_ID();", rt);
            await db.ExecuteAsync(
                @"UPDATE roomtypes SET TypeName=@TypeName,Description=@Description,MaxOccupancy=@MaxOccupancy,
                  BedType=@BedType,SizeInSqFt=@SizeInSqFt,ViewType=@ViewType,Amenities=@Amenities,
                  SortOrder=@SortOrder,IsActive=@IsActive WHERE RoomTypeId=@RoomTypeId", rt);
            return rt.RoomTypeId;
        }

        // ── ROOMS ──────────────────────────────────────────────────────────
        public async Task<IEnumerable<Room>> GetRooms(int? hotelId = null, int? rtId = null)
        {
            using var db = GetDb();
            var sql = @"SELECT r.*,rt.TypeName FROM rooms r
                        JOIN roomtypes rt ON rt.RoomTypeId=r.RoomTypeId WHERE r.IsActive=1";
            if (hotelId.HasValue) sql += " AND r.HotelId=@HotelId";
            if (rtId.HasValue)    sql += " AND r.RoomTypeId=@RtId";
            sql += " ORDER BY r.Floor,r.RoomNumber";
            return await db.QueryAsync<Room>(sql, new { HotelId = hotelId, RtId = rtId });
        }

        public async Task<int> SaveRoom(Room r)
        {
            using var db = GetDb();
            if (r.RoomId == 0)
                return await db.ExecuteScalarAsync<int>(
                    @"INSERT INTO rooms(HotelId,RoomTypeId,RoomNumber,Floor,Notes)
                      VALUES(@HotelId,@RoomTypeId,@RoomNumber,@Floor,@Notes); SELECT LAST_INSERT_ID();", r);
            await db.ExecuteAsync(
                "UPDATE rooms SET RoomTypeId=@RoomTypeId,RoomNumber=@RoomNumber,Floor=@Floor,Status=@Status,Notes=@Notes,IsActive=@IsActive WHERE RoomId=@RoomId", r);
            return r.RoomId;
        }

        // ── RATES ──────────────────────────────────────────────────────────
        public async Task<IEnumerable<RoomRate>> GetRates(int rtId, DateTime from, DateTime to)
        {
            using var db = GetDb();
            return await db.QueryAsync<RoomRate>(
                "SELECT * FROM roomrates WHERE RoomTypeId=@RtId AND RateDate BETWEEN @From AND @To ORDER BY RateDate",
                new { RtId = rtId, From = from, To = to });
        }

        public async Task BulkSaveRates(BulkRateRequest req)
        {
            using var db = GetDb();
            var d = req.FromDate;
            while (d <= req.ToDate)
            {
                await db.ExecuteAsync(
                    @"INSERT INTO roomrates(RoomTypeId,RateDate,BaseRate,SpecialRate,IsAvailable,MinNights,Notes)
                      VALUES(@RtId,@Date,@Base,@Special,@Avail,@Min,@Notes)
                      ON DUPLICATE KEY UPDATE BaseRate=@Base,SpecialRate=@Special,IsAvailable=@Avail,MinNights=@Min,Notes=@Notes",
                    new { RtId=req.RoomTypeId, Date=d, Base=req.BaseRate, Special=req.SpecialRate,
                          Avail=req.IsAvailable, Min=req.MinNights, Notes=req.Notes });
                d = d.AddDays(1);
            }
        }

        public async Task<IEnumerable<DefaultRoomRate>> GetDefaultRates(int? rtId = null)
        {
            using var db = GetDb();
            var sql = "SELECT dr.*,rt.TypeName FROM defaultroomrates dr JOIN roomtypes rt ON rt.RoomTypeId=dr.RoomTypeId WHERE 1=1";
            if (rtId.HasValue) sql += " AND dr.RoomTypeId=@RtId";
            return await db.QueryAsync<DefaultRoomRate>(sql + " ORDER BY dr.EffectiveFrom DESC", new { RtId = rtId });
        }

        public async Task<int> SaveDefaultRate(DefaultRoomRate dr)
        {
            using var db = GetDb();
            if (dr.DefaultRateId == 0)
                return await db.ExecuteScalarAsync<int>(
                    @"INSERT INTO defaultroomrates(RoomTypeId,WeekdayRate,WeekendRate,EffectiveFrom,EffectiveTo)
                      VALUES(@RoomTypeId,@WeekdayRate,@WeekendRate,@EffectiveFrom,@EffectiveTo); SELECT LAST_INSERT_ID();", dr);
            await db.ExecuteAsync(
                "UPDATE defaultroomrates SET WeekdayRate=@WeekdayRate,WeekendRate=@WeekendRate,EffectiveFrom=@EffectiveFrom,EffectiveTo=@EffectiveTo WHERE DefaultRateId=@DefaultRateId", dr);
            return dr.DefaultRateId;
        }

        // ── AVAILABILITY ───────────────────────────────────────────────────

        /// <summary>
        /// Ensures roomavailability rows exist for every room type × every date in [from, to].
        /// TotalRooms is set from the count of active 'Available' rooms for that type.
        /// Uses ON DUPLICATE KEY so it is safe to call repeatedly and never overwrites
        /// real TotalRooms values (only fills in missing rows with 0).
        ///
        /// WHY THIS IS NEEDED:
        /// roomavailability rows are only created lazily when a booking is attempted.
        /// Before any booking exists the table is empty → GetAvailability returns nothing
        /// → admin "View Availability" is blank; GetPriceQuote finds avail=0 for every
        /// date → every room shows "Unavailable" on the frontend.
        /// </summary>
        public async Task EnsureAvailabilityRows(
            System.Data.IDbConnection db, int? roomTypeId, DateTime from, DateTime to)
        {
            // Get all active room types in scope
            var rtIds = roomTypeId.HasValue
                ? new[] { roomTypeId.Value }
                : (await db.QueryAsync<int>("SELECT RoomTypeId FROM roomtypes WHERE IsActive=1")).ToArray();

            foreach (var rtId in rtIds)
            {
                // Count of active rooms = TotalRooms for this type
                var total = await db.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM rooms WHERE RoomTypeId=@RtId AND IsActive=1",
                    new { RtId = rtId });

                if (total == 0) continue; // no rooms configured — skip

                for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
                {
                    await db.ExecuteAsync(@"
                        INSERT INTO roomavailability(RoomTypeId,AvailDate,TotalRooms,BlockedRooms,BookedRooms)
                        VALUES(@RtId,@D,@Total,0,0)
                        ON DUPLICATE KEY UPDATE
                            TotalRooms=IF(TotalRooms=0 OR TotalRooms<@Total, @Total, TotalRooms)",
                        new { RtId = rtId, D = d, Total = total });
                }
            }
        }

        public async Task<IEnumerable<RoomAvailability>> GetAvailability(int? rtId, DateTime from, DateTime to)
        {
            using var db = GetDb();
            // Auto-provision missing rows so the grid is never empty just because
            // no booking has ever been attempted for this date range.
            await EnsureAvailabilityRows(db, rtId, from, to);

            var sql = @"SELECT ra.*,rt.TypeName FROM roomavailability ra
                        JOIN roomtypes rt ON rt.RoomTypeId=ra.RoomTypeId
                        WHERE ra.AvailDate BETWEEN @From AND @To";
            if (rtId.HasValue) sql += " AND ra.RoomTypeId=@RtId";
            sql += " ORDER BY ra.AvailDate,rt.SortOrder";
            return await db.QueryAsync<RoomAvailability>(sql, new { From=from, To=to, RtId=rtId });
        }

        /// <summary>
        /// Public entry point for POST /api/availability/init.
        /// Opens its own connection and delegates to EnsureAvailabilityRows.
        /// </summary>
        public async Task InitAvailability(int? roomTypeId, DateTime from, DateTime to)
        {
            using var db = GetDb();
            await EnsureAvailabilityRows(db, roomTypeId, from, to);
        }

        public async Task BlockAvailability(BlockAvailabilityRequest req)
        {
            using var db = GetDb();
            var d = req.FromDate;
            while (d <= req.ToDate)
            {
                await db.ExecuteAsync(
                    "UPDATE roomavailability SET BlockedRooms=LEAST(BlockedRooms+@Count,TotalRooms) WHERE RoomTypeId=@RtId AND AvailDate=@Date",
                    new { RtId=req.RoomTypeId, Date=d, Count=req.BlockCount });
                d = d.AddDays(1);
            }
        }

        public async Task UnblockAvailability(BlockAvailabilityRequest req)
        {
            using var db = GetDb();
            var d = req.FromDate;
            while (d <= req.ToDate)
            {
                await db.ExecuteAsync(
                    "UPDATE roomavailability SET BlockedRooms=GREATEST(BlockedRooms-@Count,0) WHERE RoomTypeId=@RtId AND AvailDate=@Date",
                    new { RtId=req.RoomTypeId, Date=d, Count=req.BlockCount });
                d = d.AddDays(1);
            }
        }

        // ── CHANNEL PARTNERS ───────────────────────────────────────────────
        public async Task<IEnumerable<ChannelPartner>> GetChannelPartners(int hotelId, bool? activeOnly = true)
        {
            using var db = GetDb();
            var sql = "SELECT * FROM channelpartners WHERE HotelId=@HId" + (activeOnly == true ? " AND IsActive=1" : "");
            return await db.QueryAsync<ChannelPartner>(sql + " ORDER BY PartnerName", new { HId = hotelId });
        }

        public async Task<ChannelPartner?> GetChannelPartner(int id)
        {
            using var db = GetDb();
            return await db.QueryFirstOrDefaultAsync<ChannelPartner>("SELECT * FROM channelpartners WHERE PartnerId=@Id", new { Id = id });
        }

        public async Task<int> SaveChannelPartner(ChannelPartner cp)
        {
            using var db = GetDb();
            if (cp.PartnerId == 0)
                return await db.ExecuteScalarAsync<int>(
                    @"INSERT INTO channelpartners(HotelId,PartnerName,PartnerCode,PartnerType,Description,APIKey,APISecret,
                      WebhookURL,CommissionPercent,PaymentMode,RemittanceDays,ContractStartDate,ContractEndDate,
                      ContactName,ContactEmail,ContactPhone)
                      VALUES(@HotelId,@PartnerName,@PartnerCode,@PartnerType,@Description,@APIKey,@APISecret,
                      @WebhookURL,@CommissionPercent,@PaymentMode,@RemittanceDays,@ContractStartDate,@ContractEndDate,
                      @ContactName,@ContactEmail,@ContactPhone); SELECT LAST_INSERT_ID();", cp);
            await db.ExecuteAsync(
                @"UPDATE channelpartners SET PartnerName=@PartnerName,PartnerType=@PartnerType,Description=@Description,
                  APIKey=@APIKey,APISecret=@APISecret,WebhookURL=@WebhookURL,CommissionPercent=@CommissionPercent,
                  PaymentMode=@PaymentMode,RemittanceDays=@RemittanceDays,ContractStartDate=@ContractStartDate,
                  ContractEndDate=@ContractEndDate,ContactName=@ContactName,ContactEmail=@ContactEmail,
                  ContactPhone=@ContactPhone,IsActive=@IsActive WHERE PartnerId=@PartnerId", cp);
            return cp.PartnerId;
        }

        public async Task<IEnumerable<ChannelRateMapping>> GetChannelRateMappings(int? partnerId = null)
        {
            using var db = GetDb();
            return await db.QueryAsync<ChannelRateMapping>(
                @"SELECT crm.*,cp.PartnerName,rt.TypeName FROM channelratemappings crm
                  JOIN channelpartners cp ON cp.PartnerId=crm.PartnerId
                  JOIN roomtypes rt ON rt.RoomTypeId=crm.RoomTypeId
                  WHERE crm.IsActive=1" + (partnerId.HasValue ? " AND crm.PartnerId=@PId" : "") +
                  " ORDER BY cp.PartnerName,rt.TypeName",
                new { PId = partnerId });
        }

        public async Task SaveChannelRateMapping(ChannelRateMapping m)
        {
            using var db = GetDb();
            await db.ExecuteAsync(
                @"INSERT INTO channelratemappings(PartnerId,RoomTypeId,MarkupPercent)
                  VALUES(@PartnerId,@RoomTypeId,@MarkupPercent)
                  ON DUPLICATE KEY UPDATE MarkupPercent=@MarkupPercent,IsActive=1", m);
        }

        // ── CUSTOMERS ──────────────────────────────────────────────────────
        public async Task<Customer?> FindCustomerByEmail(string email)
        {
            using var db = GetDb();
            return await db.QueryFirstOrDefaultAsync<Customer>("SELECT * FROM customers WHERE Email=@Email", new { Email = email });
        }

        public async Task<Customer?> GetCustomer(int id)
        {
            using var db = GetDb();
            return await db.QueryFirstOrDefaultAsync<Customer>("SELECT * FROM customers WHERE CustomerId=@Id", new { Id = id });
        }

        public async Task<(IEnumerable<Customer> items, int total)> SearchCustomers(string? q, int page, int size)
        {
            using var db = GetDb();
            var where = "WHERE 1=1";
            if (!string.IsNullOrEmpty(q))
                where += " AND (FirstName LIKE @Q OR LastName LIKE @Q OR Email LIKE @Q OR Phone LIKE @Q OR IDNumber LIKE @Q)";
            var total = await db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM customers {where}", new { Q = $"%{q}%" });
            var items = await db.QueryAsync<Customer>(
                $"SELECT * FROM customers {where} ORDER BY TotalStays DESC,CreatedAt DESC LIMIT {size} OFFSET {(page-1)*size}",
                new { Q = $"%{q}%" });
            return (items, total);
        }

        public async Task<int> SaveCustomer(Customer c)
        {
            using var db = GetDb();
            if (c.CustomerId == 0)
            {
                var existing = await FindCustomerByEmail(c.Email);
                if (existing != null) return existing.CustomerId;
                return await db.ExecuteScalarAsync<int>(
                    @"INSERT INTO customers(FirstName,LastName,Email,Phone,AlternatePhone,Address,City,State,Country,
                      ZipCode,IDType,IDNumber,Nationality,Gender)
                      VALUES(@FirstName,@LastName,@Email,@Phone,@AlternatePhone,@Address,@City,@State,@Country,
                      @ZipCode,@IDType,@IDNumber,@Nationality,@Gender); SELECT LAST_INSERT_ID();", c);
            }
            await db.ExecuteAsync(
                @"UPDATE customers SET FirstName=@FirstName,LastName=@LastName,Phone=@Phone,AlternatePhone=@AlternatePhone,
                  Address=@Address,City=@City,State=@State,Country=@Country,ZipCode=@ZipCode,IDType=@IDType,
                  IDNumber=@IDNumber,Nationality=@Nationality,Gender=@Gender,VIPStatus=@VIPStatus,Notes=@Notes
                  WHERE CustomerId=@CustomerId", c);
            return c.CustomerId;
        }

        // ── BOOKINGS ───────────────────────────────────────────────────────
        public async Task<(int bookingId, string bookingRef, string message)> CreateBooking(CreateBookingRequest req)
        {
            using var db = GetDb();
            try
            {

            // 1. Upsert customer (match by email to avoid duplicates)
            var customer = new Customer {
                FirstName=req.GuestFirstName, LastName=req.GuestLastName, Email=req.GuestEmail,
                Phone=req.GuestPhone, Nationality=req.GuestNationality, IDType=req.GuestIDType,
                IDNumber=req.GuestIDNumber, Address=req.GuestAddress, City=req.GuestCity, Country=req.GuestCountry
            };
            var custId = await SaveCustomer(customer);

            // 2. Validate nights
            var checkIn  = req.CheckInDate.Date;
            var checkOut = req.CheckOutDate.Date;
            int nights   = (checkOut - checkIn).Days;
            if (nights <= 0)
                return (0, "", "ERROR: Check-out must be after check-in");

            // 3. Availability check + auto-provision missing rows
            var activeRooms = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM rooms WHERE RoomTypeId=@RtId AND Status='Available'",
                new { RtId = req.RoomTypeId });

            for (var d = checkIn; d < checkOut; d = d.AddDays(1))
            {
                // Insert row if missing (safe — ON DUPLICATE KEY does nothing to existing)
                await db.ExecuteAsync(@"
                    INSERT INTO roomavailability(RoomTypeId,AvailDate,TotalRooms,BlockedRooms,BookedRooms)
                    VALUES(@RtId,@D,@Total,0,0)
                    ON DUPLICATE KEY UPDATE TotalRooms=IF(TotalRooms=0,@Total,TotalRooms)",
                    new { RtId = req.RoomTypeId, D = d, Total = activeRooms });

                var avail = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TotalRooms,BlockedRooms,BookedRooms FROM roomavailability WHERE RoomTypeId=@RtId AND AvailDate=@D",
                    new { RtId = req.RoomTypeId, D = d });

                if (avail == null) return (0, "", $"ERROR: No availability record for {d:yyyy-MM-dd}");
                int free = (int)avail.TotalRooms - (int)avail.BlockedRooms - (int)avail.BookedRooms;
                if (free <= 0)
                    return (0, "", $"ERROR: Room not available on {d:dd MMM yyyy}");
            }

            // 4. Fetch hotel tax rate (capped at 50%)
            var hotel = await db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TaxPercent FROM hotels WHERE HotelId=@HId",
                new { HId = req.HotelId });
            decimal taxPct = hotel != null ? Math.Min((decimal)hotel.TaxPercent, 50m) : 12m;

            // 5. Fetch partner commission rate
            decimal commPct = 0m;
            if (req.PartnerId.HasValue && req.PartnerId > 0)
            {
                var partner = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT CommissionPercent FROM channelpartners WHERE PartnerId=@PId",
                    new { PId = req.PartnerId });
                if (partner != null) commPct = (decimal)partner.CommissionPercent;
            }

            // 6. Sum nightly rates — use admin override if supplied, otherwise standard rates
            decimal subTotal = 0m;
            bool rateOverridden = req.OverrideRoomRate.HasValue && req.OverrideRoomRate.Value > 0;
            for (var d = checkIn; d < checkOut; d = d.AddDays(1))
                subTotal += rateOverridden
                    ? req.OverrideRoomRate!.Value
                    : await GetEffectiveRate(db, req.RoomTypeId, d, req.PartnerId);

            decimal taxAmount    = Math.Round(subTotal * taxPct / 100m, 2);

            // 6b. Compute add-on totals
            var addonItems  = req.Addons?.Count > 0
                ? await BuildAddonItems(db, 0, req.Addons, nights, req.AdultsCount)
                : new List<BookingAddonItem>();
            decimal addonTotal = addonItems.Sum(a => a.LineTotal);

            decimal grandTotal   = subTotal + taxAmount + addonTotal;
            decimal commAmount   = Math.Round(grandTotal * commPct / 100m, 2);
            decimal netToHotel   = grandTotal - commAmount;

            // 7. Generate booking reference and insert
            var bookingRef = $"BK{DateTime.UtcNow:yyyyMMddHHmmss}{req.RoomTypeId:D2}";
            var bookingId  = await db.ExecuteScalarAsync<int>(@"
                INSERT INTO bookings
                  (HotelId,RoomTypeId,CustomerId,PartnerId,BookingReference,
                   CheckInDate,CheckOutDate,AdultsCount,ChildrenCount,
                   RoomRate,SubTotal,TaxAmount,DiscountAmount,AddonTotal,GrandTotal,
                   CommissionAmount,NetToHotel,PaymentMode,BookingStatus,BookingSource,
                   SpecialRequests,ConfirmedAt,CreatedAt)
                VALUES
                  (@HId,@RtId,@CId,@PId,@Ref,
                   @CIn,@COut,@Adults,@Children,
                   @RoomRate,@Sub,@Tax,0,@AddonTotal,@Grand,
                   @Comm,@Net,@PayMode,'Confirmed',@Source,
                   @SpecReqs,NOW(),NOW());
                SELECT LAST_INSERT_ID();",
                new {
                    HId=req.HotelId, RtId=req.RoomTypeId, CId=custId,
                    PId=req.PartnerId.HasValue && req.PartnerId>0 ? (object)req.PartnerId.Value : DBNull.Value,
                    Ref=bookingRef, CIn=checkIn, COut=checkOut,
                    Adults=req.AdultsCount, Children=req.ChildrenCount,
                    RoomRate=rateOverridden ? req.OverrideRoomRate!.Value : (nights>0 ? subTotal/nights : subTotal),
                    Sub=subTotal, Tax=taxAmount, AddonTotal=addonTotal, Grand=grandTotal,
                    Comm=commAmount, Net=netToHotel,
                    PayMode=req.PaymentMode ?? "PayAtHotel",
                    Source=req.BookingSource ?? "Website",
                    SpecReqs=req.SpecialRequests
                });

            if (bookingId == 0) return (0, "", "ERROR: Booking insert failed");

            // Save addon line items
            foreach (var item in addonItems)
            {
                item.BookingId = bookingId;
                await db.ExecuteAsync(@"
                    INSERT INTO bookingaddonitems
                        (BookingId,AddonId,AddonName,ChargeType,Quantity,UnitPrice,TaxPercent,TaxAmount,LineTotal)
                    VALUES
                        (@BookingId,@AddonId,@AddonName,@ChargeType,@Quantity,@UnitPrice,@TaxPercent,@TaxAmount,@LineTotal)",
                    item);
            }

            // 8. Decrement availability for each night
            for (var d = checkIn; d < checkOut; d = d.AddDays(1))
                await db.ExecuteAsync(
                    "UPDATE roomavailability SET BookedRooms=BookedRooms+1 WHERE RoomTypeId=@RtId AND AvailDate=@D",
                    new { RtId = req.RoomTypeId, D = d });

            // 9. Increment customer total stays
            await db.ExecuteAsync(
                "UPDATE customers SET TotalStays=TotalStays+1 WHERE CustomerId=@CId",
                new { CId = custId });

            return (bookingId, bookingRef, $"SUCCESS: Booking {bookingRef} confirmed");
            }
            catch (Exception ex)
            {
                return (0, "", $"ERROR: {ex.Message}");
            }
        }

        // Helper: effective nightly rate with channel markup
        private async Task<decimal> GetEffectiveRate(
            System.Data.IDbConnection db, int roomTypeId, DateTime date, int? partnerId)
        {
            // Daily override first
            var daily = await db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT COALESCE(SpecialRate,BaseRate) AS Rate
                  FROM roomrates
                  WHERE RoomTypeId=@RtId AND RateDate=@D AND IsAvailable=1
                  LIMIT 1",
                new { RtId = roomTypeId, D = date });
            if (daily != null && daily.Rate != null)
            {
                decimal rate = (decimal)daily.Rate;
                if (partnerId.HasValue && partnerId > 0)
                {
                    var map = await db.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT MarkupPercent FROM channelratemappings WHERE PartnerId=@PId AND RoomTypeId=@RtId LIMIT 1",
                        new { PId = partnerId, RtId = roomTypeId });
                    if (map != null) rate *= 1m + (decimal)map.MarkupPercent / 100m;
                }
                return Math.Round(rate, 2);
            }

            // Default rate fallback (weekday vs weekend)
            bool isWeekend = date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday;
            var def = await db.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT WeekdayRate,WeekendRate FROM defaultroomrates
                  WHERE RoomTypeId=@RtId AND EffectiveFrom<=@D
                    AND (EffectiveTo IS NULL OR EffectiveTo>=@D)
                  ORDER BY EffectiveFrom DESC LIMIT 1",
                new { RtId = roomTypeId, D = date });
            if (def != null)
            {
                decimal rate = isWeekend
                    ? (decimal)(def.WeekendRate ?? 0m)
                    : (decimal)(def.WeekdayRate ?? 0m);
                if (partnerId.HasValue && partnerId > 0)
                {
                    var map = await db.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT MarkupPercent FROM channelratemappings WHERE PartnerId=@PId AND RoomTypeId=@RtId LIMIT 1",
                        new { PId = partnerId, RtId = roomTypeId });
                    if (map != null) rate *= 1m + (decimal)map.MarkupPercent / 100m;
                }
                return Math.Round(rate, 2);
            }
            return 0m;
        }

        public async Task<(string msg, decimal charge)> CancelBooking(int bookingId, string? reason, int userId)
        {
            using var db = GetDb();
            var p = new DynamicParameters();
            p.Add("p_BookingId", bookingId); p.Add("p_Reason", reason ?? "Guest requested cancellation");
            p.Add("p_By", userId);
            p.Add("p_Msg", dbType: DbType.String, size: 300, direction: ParameterDirection.Output);
            p.Add("p_Charge", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            await db.ExecuteAsync("sp_CancelBooking", p, commandType: CommandType.StoredProcedure);
            return (p.Get<string>("p_Msg") ?? "", p.Get<decimal>("p_Charge"));
        }

        // ── SAFE BOOKING SELECT ────────────────────────────────────────────
        // MySQL 9 changed how derived/computed columns in views are typed.
        // Expressions like (GrandTotal - AmountPaid) return DECIMAL(65,30) and
        // DATEDIFF() returns BIGINT — both crash Dapper mapping to C# decimal/int.
        // Explicit CAST on every derived column fixes this for MySQL 8.4 and 9.x.
        private const string BookingSelectSafe = @"
            SELECT
                BookingId, BookingReference, HotelId, RoomTypeId,
                RoomId, CustomerId, PartnerId,
                CheckInDate, CheckOutDate,
                CAST(TotalNights        AS SIGNED)        AS TotalNights,
                AdultsCount, ChildrenCount,
                CAST(RoomRate           AS DECIMAL(10,2)) AS RoomRate,
                CAST(SubTotal           AS DECIMAL(10,2)) AS SubTotal,
                CAST(TaxAmount          AS DECIMAL(10,2)) AS TaxAmount,
                CAST(DiscountAmount     AS DECIMAL(10,2)) AS DiscountAmount,
                CAST(GrandTotal         AS DECIMAL(10,2)) AS GrandTotal,
                CAST(AddonTotal         AS DECIMAL(10,2)) AS AddonTotal,
                CAST(CommissionAmount   AS DECIMAL(10,2)) AS CommissionAmount,
                CAST(NetToHotel         AS DECIMAL(10,2)) AS NetToHotel,
                CAST(AmountPaid         AS DECIMAL(10,2)) AS AmountPaid,
                CAST(BalanceDue         AS DECIMAL(10,2)) AS BalanceDue,
                PaymentMode, BookingStatus, BookingSource,
                SpecialRequests, InternalNotes,
                CancellationReason,
                CAST(CancellationCharge AS DECIMAL(10,2)) AS CancellationCharge,
                CancelledAt, CheckedInAt, CheckedOutAt,
                ConfirmedAt, CreatedAt,
                HotelName, CurrencyCode,
                RoomTypeName, RoomNumber,
                GuestName, GuestEmail, GuestPhone,
                Nationality, IDType, IDNumber, VIPStatus,
                ChannelName, PartnerCode
            FROM vw_bookingdetails";

        public async Task<Booking?> GetBooking(int id)
        {
            using var db = GetDb();
            return await db.QueryFirstOrDefaultAsync<Booking>(
                $"{BookingSelectSafe} WHERE BookingId=@Id", new { Id = id });
        }

        public async Task<Booking?> GetBookingByRef(string refNo)
        {
            using var db = GetDb();
            return await db.QueryFirstOrDefaultAsync<Booking>(
                $"{BookingSelectSafe} WHERE BookingReference=@Ref", new { Ref = refNo });
        }

        // ── CUSTOMER PORTAL — all bookings for a given guest email ─────────
        public async Task<IEnumerable<Booking>> GetBookingsByEmail(string email)
        {
            using var db = GetDb();
            return await db.QueryAsync<Booking>(
                $"{BookingSelectSafe} WHERE GuestEmail=@Email ORDER BY CreatedAt DESC",
                new { Email = email });
        }

        public async Task<(IEnumerable<Booking> items, int total)> GetBookings(ReportFilter f)
        {
            using var db = GetDb();
            // Use CreatedAt (physical column) instead of BookingDate (view alias) for filtering
            // — avoids ambiguous column reference when the view is inlined into a subquery.
            var where = "WHERE CreatedAt BETWEEN @From AND @To";
            if (f.PartnerId.HasValue)                   where += " AND PartnerId=@PId";
            if (!string.IsNullOrEmpty(f.Status))        where += " AND BookingStatus=@Status";
            if (!string.IsNullOrEmpty(f.BookingSource)) where += " AND BookingSource=@Source";

            // Include the full To date (up to 23:59:59) so date-only filters are inclusive
            var p = new {
                From   = f.FromDate.Date,
                To     = f.ToDate.Date.AddDays(1).AddSeconds(-1),
                PId    = f.PartnerId,
                Status = f.Status,
                Source = f.BookingSource
            };

            var total = await db.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM vw_bookingdetails {where}", p);
            var items = await db.QueryAsync<Booking>(
                $"{BookingSelectSafe} {where} ORDER BY CreatedAt DESC " +
                $"LIMIT {f.PageSize} OFFSET {(f.Page - 1) * f.PageSize}", p);
            return (items, total);
        }

        public async Task UpdateBookingStatus(int id, string status, int? roomId = null)
        {
            using var db = GetDb();
            var updates = "BookingStatus=@Status";
            if (status == "CheckedIn")  updates += ",CheckedInAt=NOW()";
            if (status == "CheckedOut") updates += ",CheckedOutAt=NOW()";
            if (roomId.HasValue)        updates += ",RoomId=@RoomId";
            await db.ExecuteAsync($"UPDATE bookings SET {updates} WHERE BookingId=@Id",
                new { Id = id, Status = status, RoomId = roomId });
        }

        public async Task UpdateBookingNotes(int id, string? notes)
        {
            using var db = GetDb();
            await db.ExecuteAsync("UPDATE bookings SET InternalNotes=@Notes WHERE BookingId=@Id", new { Id = id, Notes = notes });
        }

        // ── PRICE QUOTE ────────────────────────────────────────────────────
        public async Task<PriceQuoteResponse> GetPriceQuote(PriceQuoteRequest req)
        {
            using var db = GetDb();
            var rt    = await GetRoomType(req.RoomTypeId);
            var hotel = await db.QueryFirstOrDefaultAsync<Hotel>("SELECT * FROM hotels WHERE HotelId=1");
            var quote = new PriceQuoteResponse {
                RoomTypeId=req.RoomTypeId, RoomTypeName=rt?.TypeName??"",
                CheckInDate=req.CheckInDate, CheckOutDate=req.CheckOutDate,
                Nights=(int)(req.CheckOutDate-req.CheckInDate).TotalDays,
                TaxPercent=hotel?.TaxPercent??12, CurrencyCode=hotel?.CurrencyCode??"INR", IsAvailable=true
            };
            decimal commRate = 0;
            if (req.PartnerId.HasValue && req.PartnerId > 0)
            {
                var partner = await GetChannelPartner(req.PartnerId.Value);
                commRate = partner?.CommissionPercent ?? 0;
                quote.CommissionPercent = commRate;
            }

            // Auto-provision availability rows for this date range so that rooms with
            // no prior bookings are not falsely shown as "Unavailable" on the frontend.
            // This is the root cause of the "all rooms unavailable" bug on a fresh DB.
            await EnsureAvailabilityRows(db, req.RoomTypeId, req.CheckInDate, req.CheckOutDate.AddDays(-1));

            decimal subTotal = 0;
            var d = req.CheckInDate;
            while (d < req.CheckOutDate)
            {
                var rate = await GetEffectiveRate(db, req.RoomTypeId, d, req.PartnerId);
                var avail = await db.ExecuteScalarAsync<int>(
                    "SELECT COALESCE(TotalRooms-BlockedRooms-BookedRooms,0) FROM roomavailability WHERE RoomTypeId=@RtId AND AvailDate=@D",
                    new { RtId=req.RoomTypeId, D=d });
                bool isWknd = d.DayOfWeek==DayOfWeek.Friday||d.DayOfWeek==DayOfWeek.Saturday;
                quote.NightlyRates.Add(new NightlyRate { Date=d, DayName=d.ToString("ddd dd MMM"), Rate=rate, IsAvailable=avail>0, IsWeekend=isWknd });
                if (avail<=0 && quote.IsAvailable) { quote.IsAvailable=false; quote.UnavailableReason=$"No rooms available on {d:dd MMM yyyy}"; }
                subTotal += rate;
                d = d.AddDays(1);
            }
            quote.SubTotal=subTotal;
            quote.TaxAmount=Math.Round(subTotal*(hotel?.TaxPercent??12)/100,2);

            // Addon lines
            decimal addonTotal = 0m;
            if (req.Addons?.Count > 0)
            {
                foreach (var addonReq in req.Addons)
                {
                    var catalog = await db.QueryFirstOrDefaultAsync<BookingAddonCatalog>(
                        "SELECT * FROM bookingaddoncatalog WHERE AddonId=@Id AND IsAvailable=1",
                        new { Id = addonReq.AddonId });
                    if (catalog == null) continue;

                    int persons = addonReq.Quantity > 0 ? addonReq.Quantity : req.Adults;
                    decimal qty = catalog.ChargeType switch {
                        "PerNight"          => quote.Nights,
                        "PerStay"           => 1,
                        "PerPerson"         => persons,
                        "PerPersonPerNight" => persons * quote.Nights,
                        _                   => 1
                    };
                    decimal lineBase = Math.Round(catalog.UnitPrice * qty, 2);
                    decimal lineTax  = Math.Round(lineBase * catalog.TaxPercent / 100m, 2);
                    decimal lineTotal = lineBase + lineTax;
                    addonTotal += lineTotal;
                    quote.AddonLines.Add(new AddonQuoteLine {
                        AddonId    = catalog.AddonId,
                        AddonName  = catalog.AddonName,
                        ChargeType = catalog.ChargeType,
                        UnitPrice  = catalog.UnitPrice,
                        TaxPercent = catalog.TaxPercent,
                        Quantity   = qty,
                        TaxAmount  = lineTax,
                        LineTotal  = lineTotal
                    });
                }
            }
            quote.AddonTotal  = addonTotal;
            quote.GrandTotal  = subTotal + quote.TaxAmount + addonTotal;
            quote.CommissionAmount=Math.Round(subTotal*commRate/100,2);
            quote.NetToHotel=subTotal-quote.CommissionAmount;
            return quote;
        }

        // ── PAYMENTS ───────────────────────────────────────────────────────
        public async Task<int> RecordPayment(Payment pmt)
        {
            using var db = GetDb();
            var paymentId = await db.ExecuteScalarAsync<int>(
                @"INSERT INTO payments(BookingId,PaymentDate,Amount,PaymentType,PaymentMethod,
                  TransactionRef,GatewayName,GatewayTxnId,Status,Notes,ProcessedBy)
                  VALUES(@BookingId,NOW(),@Amount,@PaymentType,@PaymentMethod,
                  @TransactionRef,@GatewayName,@GatewayTxnId,@Status,@Notes,@ProcessedBy);
                  SELECT LAST_INSERT_ID();", pmt);
            await db.ExecuteAsync(
                "UPDATE bookings SET AmountPaid=LEAST(AmountPaid+@Amount,GrandTotal) WHERE BookingId=@BookingId",
                new { Amount=pmt.Amount, BookingId=pmt.BookingId });
            return paymentId;
        }

        public async Task<IEnumerable<Payment>> GetPayments(int? bookingId=null, DateTime? from=null, DateTime? to=null)
        {
            using var db = GetDb();
            var sql = @"SELECT p.*,b.BookingReference,CONCAT(c.FirstName,' ',c.LastName) AS GuestName
                        FROM payments p JOIN bookings b ON b.BookingId=p.BookingId
                        JOIN customers c ON c.CustomerId=b.CustomerId WHERE 1=1";
            if (bookingId.HasValue) sql += " AND p.BookingId=@BId";
            if (from.HasValue)      sql += " AND p.PaymentDate>=@From";
            if (to.HasValue)        sql += " AND p.PaymentDate<=@To";
            return await db.QueryAsync<Payment>(sql + " ORDER BY p.PaymentDate DESC", new { BId=bookingId, From=from, To=to });
        }

        // ── REMITTANCES ────────────────────────────────────────────────────
        public async Task<IEnumerable<PartnerRemittance>> GetRemittances(int? partnerId=null)
        {
            using var db = GetDb();
            return await db.QueryAsync<PartnerRemittance>(
                @"SELECT pr.*,cp.PartnerName FROM partnerremittances pr
                  JOIN channelpartners cp ON cp.PartnerId=pr.PartnerId WHERE 1=1" +
                  (partnerId.HasValue?" AND pr.PartnerId=@PId":"") + " ORDER BY pr.PeriodFrom DESC",
                new { PId=partnerId });
        }

        public async Task<int> SaveRemittance(PartnerRemittance r)
        {
            using var db = GetDb();
            if (r.RemittanceId==0)
                return await db.ExecuteScalarAsync<int>(
                    @"INSERT INTO partnerremittances(PartnerId,PeriodFrom,PeriodTo,TotalBookings,GrossAmount,
                      CommissionAmount,NetAmount,ReceivedAmount,Status,ExpectedDate,ReceivedDate,TransactionRef,Notes)
                      VALUES(@PartnerId,@PeriodFrom,@PeriodTo,@TotalBookings,@GrossAmount,
                      @CommissionAmount,@NetAmount,@ReceivedAmount,@Status,@ExpectedDate,@ReceivedDate,@TransactionRef,@Notes);
                      SELECT LAST_INSERT_ID();", r);
            await db.ExecuteAsync(
                "UPDATE partnerremittances SET Status=@Status,ReceivedAmount=@ReceivedAmount,ReceivedDate=@ReceivedDate,TransactionRef=@TransactionRef,Notes=@Notes WHERE RemittanceId=@RemittanceId", r);
            return r.RemittanceId;
        }

        // ── USERS ──────────────────────────────────────────────────────────
        public async Task<User?> GetUserByUsername(string username)
        {
            using var db = GetDb();
            // FIX: CAST all DATETIME columns to CHAR so MySql.Data returns strings.
            // The MySqlNullableDateTimeTypeHandler then parses them safely to DateTime?.
            // Without CAST, MySql.Data returns a MySqlDateTime object which Dapper
            // cannot map to DateTime? after it has been set to a real value (non-NULL).
            return await db.QueryFirstOrDefaultAsync<User>(
                @"SELECT UserId, HotelId, Username, PasswordHash, FullName, Email, Phone,
                         Role, IsActive,
                         CAST(LastLoginAt AS CHAR) AS LastLoginAt,
                         LoginAttempts,
                         CAST(LockedUntil  AS CHAR) AS LockedUntil,
                         MustChangePass,
                         CAST(CreatedAt    AS CHAR) AS CreatedAt
                  FROM users
                  WHERE Username=@U AND IsActive=1",
                new { U = username });
        }

        public async Task UpdateLastLogin(int userId)
        {
            using var db = GetDb();
            await db.ExecuteAsync("UPDATE users SET LastLoginAt=NOW(),LoginAttempts=0 WHERE UserId=@Id", new { Id = userId });
        }

        public async Task IncrementLoginAttempts(string username)
        {
            using var db = GetDb();
            await db.ExecuteAsync("UPDATE users SET LoginAttempts=LoginAttempts+1 WHERE Username=@U", new { U = username });
        }

        public async Task<IEnumerable<User>> GetUsers(int? hotelId=null)
        {
            using var db = GetDb();
            var sql = @"SELECT UserId, HotelId, Username, FullName, Email, Phone, Role, IsActive,
                               CAST(LastLoginAt AS CHAR) AS LastLoginAt,
                               LoginAttempts,
                               CAST(CreatedAt AS CHAR) AS CreatedAt
                        FROM users WHERE 1=1";
            if (hotelId.HasValue) sql += " AND (HotelId=@HId OR Role='SuperAdmin')";
            return await db.QueryAsync<User>(sql + " ORDER BY Username", new { HId = hotelId });
        }

        public async Task<int> CreateUser(User u)
        {
            using var db = GetDb();
            return await db.ExecuteScalarAsync<int>(
                @"INSERT INTO users(HotelId,Username,PasswordHash,FullName,Email,Phone,Role)
                  VALUES(@HotelId,@Username,@PasswordHash,@FullName,@Email,@Phone,@Role); SELECT LAST_INSERT_ID();", u);
        }

        public async Task UpdateUser(int id, string? fullName, string? email, string? phone, string? role, bool? isActive, int? hotelId)
        {
            using var db = GetDb();
            await db.ExecuteAsync(
                @"UPDATE users SET FullName=COALESCE(@FN,FullName),Email=COALESCE(@Em,Email),
                  Phone=COALESCE(@Ph,Phone),Role=COALESCE(@Role,Role),
                  IsActive=COALESCE(@Active,IsActive),HotelId=COALESCE(@HId,HotelId) WHERE UserId=@Id",
                new { Id=id, FN=fullName, Em=email, Ph=phone, Role=role, Active=isActive, HId=hotelId });
        }

        public async Task UpdateUserPassword(int id, string hash)
        {
            using var db = GetDb();
            await db.ExecuteAsync("UPDATE users SET PasswordHash=@Hash,MustChangePass=0 WHERE UserId=@Id", new { Id=id, Hash=hash });
        }

        // ── DASHBOARD ──────────────────────────────────────────────────────
        public async Task<DashboardStats> GetDashboardStats(int hotelId)
        {
            using var db = GetDb();
            var today = DateTime.Today;
            var stats = new DashboardStats();
            try { stats.TodayArrivals=await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM bookings WHERE HotelId=@H AND CheckInDate=@D AND BookingStatus IN('Confirmed','CheckedIn')",new{H=hotelId,D=today}); } catch { }
            try { stats.TodayDepartures=await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM bookings WHERE HotelId=@H AND CheckOutDate=@D AND BookingStatus='CheckedIn'",new{H=hotelId,D=today}); } catch { }
            try { stats.CurrentlyOccupied=await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM bookings WHERE HotelId=@H AND BookingStatus='CheckedIn'",new{H=hotelId}); } catch { }
            try { stats.TodayRevenue=await db.ExecuteScalarAsync<decimal>("SELECT COALESCE(SUM(GrandTotal),0) FROM bookings WHERE HotelId=@H AND DATE(CreatedAt)=@D AND BookingStatus<>'Cancelled'",new{H=hotelId,D=today}); } catch { }
            try { stats.MonthRevenue=await db.ExecuteScalarAsync<decimal>("SELECT COALESCE(SUM(GrandTotal),0) FROM bookings WHERE HotelId=@H AND YEAR(CreatedAt)=YEAR(@D) AND MONTH(CreatedAt)=MONTH(@D) AND BookingStatus<>'Cancelled'",new{H=hotelId,D=today}); } catch { }
            try { stats.YearRevenue=await db.ExecuteScalarAsync<decimal>("SELECT COALESCE(SUM(GrandTotal),0) FROM bookings WHERE HotelId=@H AND YEAR(CreatedAt)=YEAR(@D) AND BookingStatus<>'Cancelled'",new{H=hotelId,D=today}); } catch { }
            try { stats.TotalBookingsThisMonth=await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM bookings WHERE HotelId=@H AND YEAR(CreatedAt)=YEAR(@D) AND MONTH(CreatedAt)=MONTH(@D)",new{H=hotelId,D=today}); } catch { }
            try { stats.PendingCheckouts=await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM bookings WHERE HotelId=@H AND CheckOutDate<=@D AND BookingStatus='CheckedIn'",new{H=hotelId,D=today}); } catch { }
            try { var av=(await GetAvailability(null,today,today)).ToList(); stats.TotalRooms=av.Sum(a=>a.TotalRooms); stats.TotalAvailableRooms=av.Sum(a=>a.AvailableRooms); stats.OccupancyRate=stats.TotalRooms>0?Math.Round((decimal)av.Sum(a=>a.BookedRooms)*100/stats.TotalRooms,1):0; } catch { }
            try { stats.ChannelSummary=(await db.QueryAsync<ChannelRevenueSummary>("SELECT * FROM vw_channelrevenuesummary")).AsList(); } catch { stats.ChannelSummary=new(); }
            // RoomOccupancy: use GetAvailability so rows are auto-provisioned if missing
            try {
                var avToday = (await GetAvailability(hotelId == 0 ? (int?)null : null, today, today)).ToList();
                stats.RoomOccupancy = avToday.Select(a => new RoomTypeOccupancy {
                    TypeName       = a.TypeName,
                    TotalRooms     = a.TotalRooms,
                    BlockedRooms   = a.BlockedRooms,
                    BookedRooms    = a.BookedRooms,
                    AvailableRooms = a.AvailableRooms,
                    OccupancyPct   = a.TotalRooms > 0
                        ? Math.Round(a.BookedRooms * 100.0m / a.TotalRooms, 1) : 0
                }).ToList();
            } catch { stats.RoomOccupancy = new(); }
            try { stats.RecentBookings=(await db.QueryAsync<RecentBooking>(@"
                SELECT BookingId, BookingReference, GuestName, RoomTypeName,
                       CheckInDate, CAST(GrandTotal AS DECIMAL(10,2)) AS GrandTotal,
                       BookingStatus, ChannelName, CreatedAt
                FROM vw_bookingdetails ORDER BY CreatedAt DESC LIMIT 10")).AsList(); } catch { stats.RecentBookings=new(); }
            return stats;
        }

        // ── SYSTEM SETTINGS ────────────────────────────────────────────────
        public async Task<IEnumerable<SystemSetting>> GetSettings()
        {
            using var db = GetDb();
            return await db.QueryAsync<SystemSetting>("SELECT * FROM systemsettings ORDER BY SettingKey");
        }

        public async Task SaveSetting(string key, string? value)
        {
            using var db = GetDb();
            await db.ExecuteAsync("INSERT INTO systemsettings(SettingKey,SettingValue) VALUES(@K,@V) ON DUPLICATE KEY UPDATE SettingValue=@V", new { K=key, V=value });
        }

        // ── AUDIT LOG ──────────────────────────────────────────────────────
        public async Task LogAudit(int? userId, string action, string? module=null, string? recordId=null, string? notes=null)
        {
            try
            {
                using var db = GetDb();
                await db.ExecuteAsync("INSERT INTO auditlogs(UserId,Action,Module,RecordId,Notes) VALUES(@U,@A,@M,@R,@N)",
                    new { U=userId, A=action, M=module, R=recordId, N=notes });
            }
            catch { }
        }

        public async Task<IEnumerable<dynamic>> GetAuditLogs(int page=1, int size=50)
        {
            using var db = GetDb();
            return await db.QueryAsync("SELECT al.*,u.Username FROM auditlogs al LEFT JOIN users u ON u.UserId=al.UserId ORDER BY al.CreatedAt DESC LIMIT @Size OFFSET @Offset",
                new { Size=size, Offset=(page-1)*size });
        }

        // ══════════════════════════════════════════════════════════════════════
        // ORDER MANAGEMENT SYSTEM (OMS)
        // ══════════════════════════════════════════════════════════════════════

        // ══════════════════════════════════════════════════════════════════════
        // BOOKING ADD-ONS
        // ══════════════════════════════════════════════════════════════════════

        public async Task<IEnumerable<BookingAddonCatalog>> GetAddonCatalog(int hotelId, bool availableOnly = true)
        {
            using var db = GetDb();
            var sql = "SELECT * FROM bookingaddoncatalog WHERE HotelId=@HId"
                    + (availableOnly ? " AND IsAvailable=1" : "")
                    + " ORDER BY SortOrder, AddonName";
            return await db.QueryAsync<BookingAddonCatalog>(sql, new { HId = hotelId });
        }

        public async Task<int> SaveAddonCatalog(BookingAddonCatalog a)
        {
            using var db = GetDb();
            if (a.AddonId == 0)
                return await db.ExecuteScalarAsync<int>(@"
                    INSERT INTO bookingaddoncatalog
                        (HotelId,AddonName,Description,Category,ChargeType,UnitPrice,TaxPercent,IsAvailable,SortOrder)
                    VALUES
                        (@HotelId,@AddonName,@Description,@Category,@ChargeType,@UnitPrice,@TaxPercent,@IsAvailable,@SortOrder);
                    SELECT LAST_INSERT_ID();", a);
            await db.ExecuteAsync(@"
                UPDATE bookingaddoncatalog
                SET AddonName=@AddonName, Description=@Description, Category=@Category,
                    ChargeType=@ChargeType, UnitPrice=@UnitPrice, TaxPercent=@TaxPercent,
                    IsAvailable=@IsAvailable, SortOrder=@SortOrder
                WHERE AddonId=@AddonId", a);
            return a.AddonId;
        }

        public async Task DeactivateAddon(int addonId)
        {
            using var db = GetDb();
            await db.ExecuteAsync(
                "UPDATE bookingaddoncatalog SET IsAvailable=0 WHERE AddonId=@Id",
                new { Id = addonId });
        }

        public async Task<IEnumerable<BookingAddonItem>> GetBookingAddons(int bookingId)
        {
            using var db = GetDb();
            return await db.QueryAsync<BookingAddonItem>(
                "SELECT * FROM bookingaddonitems WHERE BookingId=@BId ORDER BY ItemId",
                new { BId = bookingId });
        }

        private async Task<List<BookingAddonItem>> BuildAddonItems(
            System.Data.IDbConnection db,
            int bookingId,
            List<BookingAddonRequest> addonRequests,
            int nights,
            int adults)
        {
            var items = new List<BookingAddonItem>();
            foreach (var req in addonRequests)
            {
                var catalog = await db.QueryFirstOrDefaultAsync<BookingAddonCatalog>(
                    "SELECT * FROM bookingaddoncatalog WHERE AddonId=@Id AND IsAvailable=1",
                    new { Id = req.AddonId });
                if (catalog == null) continue;

                int persons  = req.Quantity > 0 ? req.Quantity : adults;
                decimal qty  = catalog.ChargeType switch {
                    "PerNight"          => nights,
                    "PerStay"           => 1,
                    "PerPerson"         => persons,
                    "PerPersonPerNight" => persons * nights,
                    _                   => 1
                };
                decimal lineBase = Math.Round(catalog.UnitPrice * qty, 2);
                decimal tax      = Math.Round(lineBase * catalog.TaxPercent / 100m, 2);
                items.Add(new BookingAddonItem {
                    BookingId  = bookingId,
                    AddonId    = catalog.AddonId,
                    AddonName  = catalog.AddonName,
                    ChargeType = catalog.ChargeType,
                    Quantity   = qty,
                    UnitPrice  = catalog.UnitPrice,
                    TaxPercent = catalog.TaxPercent,
                    TaxAmount  = tax,
                    LineTotal  = lineBase + tax
                });
            }
            return items;
        }

        // ── ORDER CATALOG ──────────────────────────────────────────────────────


        public async Task<IEnumerable<OrderCatalogItem>> GetOrderCatalog(int hotelId, string? category = null)
        {
            using var db = GetDb();
            var sql = "SELECT * FROM ordercatalog WHERE HotelId=@HId AND IsAvailable=1";
            if (!string.IsNullOrEmpty(category)) sql += " AND Category=@Cat";
            sql += " ORDER BY Category, SortOrder, ItemName";
            return await db.QueryAsync<OrderCatalogItem>(sql, new { HId = hotelId, Cat = category });
        }

        public async Task<IEnumerable<OrderCatalogItem>> GetAllOrderCatalog(int hotelId)
        {
            using var db = GetDb();
            return await db.QueryAsync<OrderCatalogItem>(
                "SELECT * FROM ordercatalog WHERE HotelId=@HId ORDER BY Category, SortOrder, ItemName",
                new { HId = hotelId });
        }

        public async Task<int> CreateCatalogItem(OrderCatalogItem item)
        {
            using var db = GetDb();
            return await db.ExecuteScalarAsync<int>(@"
                INSERT INTO ordercatalog(HotelId,Category,ItemName,Description,UnitPrice,Unit,TaxPercent,IsAvailable,ImageUrl,SortOrder)
                VALUES(@HotelId,@Category,@ItemName,@Description,@UnitPrice,@Unit,@TaxPercent,@IsAvailable,@ImageUrl,@SortOrder);
                SELECT LAST_INSERT_ID();", item);
        }

        public async Task UpdateCatalogItem(OrderCatalogItem item)
        {
            using var db = GetDb();
            await db.ExecuteAsync(@"
                UPDATE ordercatalog
                SET ItemName=@ItemName, Description=@Description, UnitPrice=@UnitPrice,
                    Unit=@Unit, TaxPercent=@TaxPercent, IsAvailable=@IsAvailable,
                    ImageUrl=@ImageUrl, SortOrder=@SortOrder
                WHERE CatalogId=@CatalogId", item);
        }

        public async Task DeleteCatalogItem(int catalogId)
        {
            using var db = GetDb();
            await db.ExecuteAsync("UPDATE ordercatalog SET IsAvailable=0 WHERE CatalogId=@Id", new { Id = catalogId });
        }

        // ── ORDERS ─────────────────────────────────────────────────────────────

        public async Task<(int OrderId, string OrderNumber, string Message)> CreateOrder(
            int hotelId, string orderType,
            int? bookingId, int? roomId, int? customerId,
            string? walkInGuestName, string? walkInGuestPhone,
            string category, string priority, string? specInstr,
            DateTime? deliveryTime, int createdBy)
        {
            using var db = GetDb();
            try
            {
                // ── Validate ─────────────────────────────────────────────────
                if (orderType == "InRoom" && (bookingId == null || bookingId == 0))
                    return (0, "", "ERROR: InRoom order requires a valid BookingId");

                if (orderType == "DirectSale" &&
                    string.IsNullOrWhiteSpace(walkInGuestName))
                    return (0, "", "ERROR: Guest name is required for Direct Sale orders");

                if (string.IsNullOrWhiteSpace(category))
                    return (0, "", "ERROR: Category is required");

                // ── For InRoom: resolve CustomerId from the booking if not supplied ──
                if (orderType == "InRoom" && (customerId == null || customerId == 0) && bookingId > 0)
                {
                    var custFromBooking = await db.ExecuteScalarAsync<int?>(
                        "SELECT CustomerId FROM bookings WHERE BookingId = @BId", new { BId = bookingId });
                    if (custFromBooking.HasValue && custFromBooking.Value > 0)
                        customerId = custFromBooking.Value;
                }

                // ── Generate order number ─────────────────────────────────────
                var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

                // ── Insert ────────────────────────────────────────────────────
                // NOTE: CustomerId is intentionally omitted from INSERT.
                // The column may be NOT NULL in older DB schemas; it is not needed
                // at the order level — guest info is resolved via BookingId JOIN
                // or WalkInGuestName for DirectSale. Run the oms_migration_fix.sql
                // to make the column nullable if you have not done so already.
                var orderId = await db.ExecuteScalarAsync<int>(@"
                    INSERT INTO orders
                      (HotelId, OrderType, BookingId, RoomId,
                       WalkInGuestName, WalkInGuestPhone,
                       OrderNumber, Category, Priority, SpecialInstructions,
                       DeliveryTime, CreatedBy, OrderStatus, CreatedAt)
                    VALUES
                      (@HotelId, @OrderType, @BookingId, @RoomId,
                       @WalkInGuestName, @WalkInGuestPhone,
                       @OrderNumber, @Category, @Priority, @SpecInstr,
                       @DeliveryTime, @CreatedBy, 'Pending', NOW());
                    SELECT LAST_INSERT_ID();",
                    new {
                        HotelId          = hotelId,
                        OrderType        = orderType,
                        BookingId        = (object?)(bookingId > 0 ? bookingId : null) ?? DBNull.Value,
                        RoomId           = (object?)(roomId    > 0 ? roomId    : null) ?? DBNull.Value,
                        WalkInGuestName  = walkInGuestName,
                        WalkInGuestPhone = walkInGuestPhone,
                        OrderNumber      = orderNumber,
                        Category         = category,
                        Priority         = priority ?? "Normal",
                        SpecInstr        = specInstr,
                        DeliveryTime     = (object?)deliveryTime ?? DBNull.Value,
                        CreatedBy        = createdBy
                    });

                if (orderId == 0) return (0, "", "ERROR: Failed to create order");

                // ── Log initial status history ────────────────────────────────
                await db.ExecuteAsync(@"
                    INSERT INTO orderstatushistory(OrderId, OldStatus, NewStatus, ChangedBy, Notes, ChangedAt)
                    VALUES(@OId, NULL, 'Pending', @By, 'Order created', NOW())",
                    new { OId = orderId, By = createdBy });

                return (orderId, orderNumber, $"SUCCESS: Order {orderNumber} created");
            }
            catch (Exception ex)
            {
                return (0, "", $"ERROR: {ex.Message}");
            }
        }

        public async Task AddOrderItem(int orderId, OrderItemRequest item)
        {
            using var db = GetDb();
            await db.ExecuteAsync(@"
                INSERT INTO orderitems(OrderId,CatalogId,ItemName,Description,Quantity,UnitPrice,TaxPercent,Notes)
                VALUES(@OrderId,@CatalogId,@ItemName,@Description,@Quantity,@UnitPrice,@TaxPercent,@Notes)",
                new {
                    OrderId = orderId, item.CatalogId, item.ItemName, item.Description,
                    item.Quantity, item.UnitPrice, item.TaxPercent, item.Notes
                });
            await RecalcOrderTotals(db, orderId);
        }

        public async Task RemoveOrderItem(int orderItemId)
        {
            using var db = GetDb();
            var orderId = await db.ExecuteScalarAsync<int>(
                "SELECT OrderId FROM orderitems WHERE OrderItemId=@Id", new { Id = orderItemId });
            await db.ExecuteAsync("DELETE FROM orderitems WHERE OrderItemId=@Id", new { Id = orderItemId });
            if (orderId > 0) await RecalcOrderTotals(db, orderId);
        }

        /// <summary>
        /// Recalculates SubTotal, TaxAmount, GrandTotal on the orders header row.
        ///
        /// IMPORTANT — orderitems GENERATED COLUMNS (read-only, MySQL computes automatically):
        ///   LineTotal        GENERATED AS (Quantity * UnitPrice)
        ///   TaxAmount        GENERATED AS (ROUND(Quantity * UnitPrice * TaxPercent / 100, 2))
        ///   LineTotalWithTax GENERATED AS (LineTotal + TaxAmount)
        ///
        /// We must NEVER write to these columns (INSERT or UPDATE will throw
        /// "value specified for generated column is not allowed").
        /// We only need to READ them and SUM into the orders header.
        ///
        /// The orders table columns (SubTotal, TaxAmount, GrandTotal) ARE regular
        /// writable columns and must be kept in sync manually.
        /// </summary>
        private static async Task RecalcOrderTotals(System.Data.IDbConnection db, int orderId)
        {
            // Read the generated column values MySQL already computed for us
            var totals = await db.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT
                    COALESCE(SUM(LineTotal),        0) AS SubTotal,
                    COALESCE(SUM(TaxAmount),        0) AS TaxTotal,
                    COALESCE(SUM(LineTotalWithTax), 0) AS GrandTotal
                FROM orderitems
                WHERE OrderId = @Id",
                new { Id = orderId });

            decimal subTotal  = totals != null ? (decimal)totals.SubTotal  : 0m;
            decimal taxTotal  = totals != null ? (decimal)totals.TaxTotal  : 0m;
            decimal grandTotal= totals != null ? (decimal)totals.GrandTotal: 0m;

            // Update only the orders header — these ARE regular writable columns
            await db.ExecuteAsync(@"
                UPDATE orders
                SET SubTotal   = @Sub,
                    TaxAmount  = @Tax,
                    GrandTotal = @Grand
                WHERE OrderId  = @Id",
                new { Sub = subTotal, Tax = taxTotal, Grand = grandTotal, Id = orderId });
        }

        public async Task<IEnumerable<OrderItem>> GetOrderItems(int orderId)
        {
            using var db = GetDb();
            return await db.QueryAsync<OrderItem>(
                "SELECT * FROM orderitems WHERE OrderId=@Id ORDER BY OrderItemId",
                new { Id = orderId });
        }

        public async Task<IEnumerable<Order>> GetOrders(
            int hotelId, string? status = null, string? category = null,
            string? orderType = null, int? bookingId = null,
            DateTime? from = null, DateTime? to = null,
            int page = 1, int pageSize = 50)
        {
            using var db = GetDb();
            // Use LEFT JOIN so DirectSale orders (no booking) are included
            var sql = "SELECT * FROM vw_OrderDetails WHERE HotelId = @HId";
            if (!string.IsNullOrEmpty(status))    sql += " AND OrderStatus=@Status";
            if (!string.IsNullOrEmpty(category))  sql += " AND Category=@Cat";
            if (!string.IsNullOrEmpty(orderType)) sql += " AND OrderType=@OType";
            if (bookingId.HasValue)               sql += " AND BookingId=@BId";
            if (from.HasValue)                    sql += " AND DATE(OrderDate)>=@From";
            if (to.HasValue)                      sql += " AND DATE(OrderDate)<=@To";
            sql += $" ORDER BY OrderDate DESC LIMIT {pageSize} OFFSET {(page - 1) * pageSize}";
            return await db.QueryAsync<Order>(sql,
                new { HId = hotelId, Status = status, Cat = category,
                      OType = orderType, BId = bookingId, From = from, To = to });
        }

        public async Task<Order?> GetOrderById(int orderId)
        {
            using var db = GetDb();
            return await db.QueryFirstOrDefaultAsync<Order>(
                "SELECT * FROM vw_OrderDetails WHERE OrderId=@Id", new { Id = orderId });
        }

        public async Task<string> UpdateOrderStatus(int orderId, string newStatus, int userId, string? notes)
        {
            using var db = GetDb();
            try
            {
                // Unified flow for ALL order types: Pending → InProgress → Delivered → Billed
                // 'Confirmed' stage has been removed — it is no longer part of the workflow.
                var allowed = new[] { "InProgress", "Delivered", "Cancelled" };
                if (!allowed.Contains(newStatus))
                    return $"ERROR: Invalid status '{newStatus}'";

                // Fetch current order
                var order = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT OrderId, OrderStatus, OrderType FROM orders WHERE OrderId = @Id",
                    new { Id = orderId });

                if (order == null)
                    return "ERROR: Order not found";

                string oldStatus = (string)order.OrderStatus;

                // Guard terminal states
                if (oldStatus == "Billed")
                    return "ERROR: Billed orders cannot be updated";
                if (oldStatus == "Cancelled")
                    return "ERROR: Cancelled orders cannot be updated";
                if (oldStatus == newStatus)
                    return $"ERROR: Order is already {newStatus}";

                // Enforce forward-only progression
                // Pending → InProgress → Delivered (Cancelled allowed from Pending or InProgress)
                var order_flow = new[] { "Pending", "InProgress", "Delivered", "Billed" };
                int oldIdx = Array.IndexOf(order_flow, oldStatus);
                int newIdx = Array.IndexOf(order_flow, newStatus);
                if (newStatus != "Cancelled" && newIdx <= oldIdx)
                    return $"ERROR: Cannot move order from '{oldStatus}' back to '{newStatus}'";

                // Build the UPDATE — set extra timestamp columns where relevant
                string extraCols = newStatus switch
                {
                    "Delivered" => ", CompletedAt = NOW()",
                    "Cancelled" => ", CancelledAt = NOW(), CancelledBy = @UserId, CancellationReason = @Notes",
                    _           => ""
                };

                await db.ExecuteAsync(
                    $"UPDATE orders SET OrderStatus = @NewStatus{extraCols} WHERE OrderId = @Id",
                    new { NewStatus = newStatus, Id = orderId, UserId = userId, Notes = notes ?? "" });

                // Insert status history row
                await db.ExecuteAsync(@"
                    INSERT INTO orderstatushistory
                        (OrderId, OldStatus, NewStatus, ChangedBy, Notes, ChangedAt)
                    VALUES
                        (@OId, @OldStatus, @NewStatus, @By, @Notes, NOW())",
                    new { OId = orderId, OldStatus = oldStatus, NewStatus = newStatus,
                          By = userId, Notes = notes ?? "" });

                // ── Auto-deduct inventory when order reaches Delivered ──────────
                if (newStatus == "Delivered")
                {
                    var orderItems = await db.QueryAsync<dynamic>(@"
                        SELECT oi.CatalogId, oi.Quantity
                        FROM   orderitems oi
                        WHERE  oi.OrderId = @Id AND oi.CatalogId IS NOT NULL",
                        new { Id = orderId });

                    var shortageWarnings = new List<string>();

                    foreach (var oi in orderItems)
                    {
                        int catalogId = (int)oi.CatalogId;
                        int portions  = (int)Math.Max(1, Math.Round((double)oi.Quantity));

                        var recipe = await db.QueryFirstOrDefaultAsync<dynamic>(
                            "SELECT RecipeId, RecipeName FROM recipes WHERE CatalogId=@CId AND IsActive=1 LIMIT 1",
                            new { CId = catalogId });

                        if (recipe == null) continue;

                        var orderHotelId = await db.ExecuteScalarAsync<int>(
                            "SELECT HotelId FROM orders WHERE OrderId=@Id", new { Id = orderId });

                        var deductReq = new DeductRecipeStockRequest
                        {
                            RecipeId = (int)recipe.RecipeId,
                            Portions = portions,
                            OrderId  = orderId,
                            Notes    = $"Auto-deduct on Delivered — Order #{orderId}"
                        };

                        var (ok, msg) = await DeductRecipeStock(orderHotelId, userId, deductReq);
                        if (!ok)
                            shortageWarnings.Add($"{recipe.RecipeName}: {msg}");
                    }

                    if (shortageWarnings.Any())
                        return $"WARNING: Order marked Delivered but inventory could not be fully deducted — " +
                               string.Join("; ", shortageWarnings);
                }

                return $"SUCCESS: Order status updated to {newStatus}";
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        public async Task<(int BillEntryId, string ReceiptNo, string Message)> BillOrder(int orderId, int postedBy)
        {
            using var db = GetDb();
            try
            {
                var order = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT OrderId, OrderType, BookingId, GrandTotal, SubTotal, TaxAmount, OrderStatus, OrderNumber FROM orders WHERE OrderId=@Id",
                    new { Id = orderId });

                if (order == null)       return (0, "", "ERROR: Order not found");
                if ((string)order.OrderStatus != "Delivered")
                    return (0, "", "ERROR: Only Delivered orders can be billed");

                if ((string)order.OrderType == "InRoom")
                {
                    // Post charge to the booking folio
                    var billId = await db.ExecuteScalarAsync<int>(@"
                        INSERT INTO billentries
                          (BookingId, EntryType, Description, ReferenceId, ReferenceType,
                           Amount, TaxAmount, GrandAmount, PostedBy)
                        VALUES
                          (@BId, 'OrderCharge', @Desc, @RefId, 'Order',
                           @Amount, @Tax, @Grand, @By);
                        SELECT LAST_INSERT_ID();",
                        new {
                            BId    = (int)order.BookingId,
                            Desc   = $"Order {(string)order.OrderNumber}",
                            RefId  = orderId,
                            Amount = (decimal)order.SubTotal,
                            Tax    = (decimal)order.TaxAmount,
                            Grand  = (decimal)order.GrandTotal,
                            By     = postedBy
                        });
                    await db.ExecuteAsync(
                        "UPDATE orders SET OrderStatus='Billed', BillEntryId=@BillId WHERE OrderId=@Id",
                        new { BillId = billId, Id = orderId });
                    return (billId, "", $"SUCCESS: Order billed to folio. BillEntry #{billId}");
                }
                else
                {
                    // DirectSale — generate a standalone receipt number
                    var receiptNo = $"RCP-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                    await db.ExecuteAsync(
                        "UPDATE orders SET OrderStatus='Billed', DirectReceiptNo=@Rcp, BillEntryId=0 WHERE OrderId=@Id",
                        new { Rcp = receiptNo, Id = orderId });
                    return (0, receiptNo, $"SUCCESS: Receipt {receiptNo} generated");
                }
            }
            catch (Exception ex)
            {
                return (0, "", $"ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Bill a DirectSale order that has reached Delivered status.
        /// Generates a standalone receipt number and marks the order as Billed.
        /// The order must be in Delivered status — same rule as InRoom BillOrder.
        /// The frontend handles the Pending→InProgress→Delivered flow via UpdateOrderStatus.
        /// </summary>
        public async Task<(int BillEntryId, string ReceiptNo, string Message)> QuickBillDirectSale(int orderId, int postedBy)
        {
            using var db = GetDb();
            try
            {
                var order = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT OrderId, OrderType, BookingId, GrandTotal, SubTotal, TaxAmount, OrderStatus, OrderNumber FROM orders WHERE OrderId=@Id",
                    new { Id = orderId });

                if (order == null) return (0, "", "ERROR: Order not found");
                if ((string)order.OrderType != "DirectSale")
                    return (0, "", "ERROR: This endpoint is only for Direct Sale orders");
                if ((string)order.OrderStatus == "Billed")
                    return (0, "", "ERROR: Order is already billed");
                if ((string)order.OrderStatus == "Cancelled")
                    return (0, "", "ERROR: Cancelled orders cannot be billed");
                if ((string)order.OrderStatus != "Delivered")
                    return (0, "", "ERROR: Only Delivered orders can be billed. Please mark the order as Delivered first.");

                // Generate receipt and mark Billed
                var receiptNo = $"RCP-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                await db.ExecuteAsync(@"
                    UPDATE orders
                    SET OrderStatus='Billed', DirectReceiptNo=@Rcp, BillEntryId=0
                    WHERE OrderId=@Id",
                    new { Rcp = receiptNo, Id = orderId });

                await db.ExecuteAsync(@"
                    INSERT INTO orderstatushistory(OrderId, OldStatus, NewStatus, ChangedBy, Notes, ChangedAt)
                    VALUES(@OId, 'Delivered', 'Billed', @By, @Rcp, NOW())",
                    new { OId = orderId, By = postedBy, Rcp = $"Receipt: {receiptNo}" });

                return (0, receiptNo, $"SUCCESS: Receipt {receiptNo} generated");
            }
            catch (Exception ex)
            {
                return (0, "", $"ERROR: {ex.Message}");
            }
        }

        public async Task<IEnumerable<OrderStatusHistory>> GetOrderHistory(int orderId)
        {
            using var db = GetDb();
            return await db.QueryAsync<OrderStatusHistory>(@"
                SELECT h.*, u.FullName AS ChangedByName
                FROM orderstatushistory h
                LEFT JOIN users u ON u.UserId = h.ChangedBy
                WHERE h.OrderId = @Id ORDER BY h.ChangedAt DESC", new { Id = orderId });
        }

        public async Task<OrderSummaryStats> GetOrderStats(int hotelId, DateTime? from = null, DateTime? to = null)
        {
            using var db = GetDb();
            from ??= DateTime.Today;
            to   ??= DateTime.Today;
            return await db.QueryFirstOrDefaultAsync<OrderSummaryStats>(@"
                SELECT
                  COUNT(*) AS TotalOrders,
                  SUM(CASE WHEN o.OrderStatus='Pending'    THEN 1 ELSE 0 END) AS PendingOrders,
                  SUM(CASE WHEN o.OrderStatus='InProgress' THEN 1 ELSE 0 END) AS InProgressOrders,
                  SUM(CASE WHEN o.OrderStatus='Delivered'  THEN 1 ELSE 0 END) AS DeliveredOrders,
                  SUM(CASE WHEN o.OrderStatus='Billed'     THEN 1 ELSE 0 END) AS BilledOrders,
                  SUM(CASE WHEN o.OrderStatus='Cancelled'  THEN 1 ELSE 0 END) AS CancelledOrders,
                  COALESCE(SUM(CASE WHEN o.OrderStatus<>'Cancelled' THEN o.GrandTotal ELSE 0 END),0) AS TotalRevenue,
                  COALESCE(SUM(CASE WHEN o.OrderStatus NOT IN('Cancelled','Billed') THEN o.GrandTotal ELSE 0 END),0) AS UnbilledAmount
                FROM orders o
                WHERE o.HotelId = @HId
                  AND DATE(o.CreatedAt) BETWEEN @From AND @To",
                new { HId = hotelId, From = from, To = to }) ?? new OrderSummaryStats();
        }

        // ── OMS REPORTS ────────────────────────────────────────────────────────

        /// <summary>Detailed order list for the OMS report page, with full filters.</summary>
        public async Task<(IEnumerable<Order> Items, int Total)> GetOrderReport(
            int hotelId, DateTime from, DateTime to,
            string? orderType, string? category, string? status,
            int page, int pageSize)
        {
            using var db = GetDb();
            var where = "WHERE HotelId=@HId AND DATE(OrderDate) BETWEEN @From AND @To";
            if (!string.IsNullOrEmpty(orderType)) where += " AND OrderType=@OType";
            if (!string.IsNullOrEmpty(category))  where += " AND Category=@Cat";
            if (!string.IsNullOrEmpty(status))    where += " AND OrderStatus=@Status";

            var total = await db.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM vw_OrderDetails {where}",
                new { HId=hotelId, From=from, To=to, OType=orderType, Cat=category, Status=status });

            var items = await db.QueryAsync<Order>(
                $"SELECT * FROM vw_OrderDetails {where} ORDER BY OrderDate DESC LIMIT {pageSize} OFFSET {(page-1)*pageSize}",
                new { HId=hotelId, From=from, To=to, OType=orderType, Cat=category, Status=status });

            return (items, total);
        }

        /// <summary>Revenue grouped by category for the date range.</summary>
        public async Task<IEnumerable<dynamic>> GetOrderRevenueByCategory(
            int hotelId, DateTime from, DateTime to, string? orderType)
        {
            using var db = GetDb();
            var where = "WHERE HotelId=@HId AND DATE(CreatedAt) BETWEEN @From AND @To AND OrderStatus <> 'Cancelled'";
            if (!string.IsNullOrEmpty(orderType)) where += " AND OrderType=@OType";
            return await db.QueryAsync(
                $@"SELECT Category,
                          COUNT(*) AS OrderCount,
                          COALESCE(SUM(SubTotal),0)    AS SubTotal,
                          COALESCE(SUM(TaxAmount),0)   AS TaxAmount,
                          COALESCE(SUM(GrandTotal),0)  AS Revenue
                   FROM orders {where}
                   GROUP BY Category
                   ORDER BY Revenue DESC",
                new { HId=hotelId, From=from, To=to, OType=orderType });
        }

        /// <summary>Daily revenue totals for the trend chart.</summary>
        public async Task<IEnumerable<dynamic>> GetOrderRevenueByDay(
            int hotelId, DateTime from, DateTime to, string? orderType)
        {
            using var db = GetDb();
            var where = "WHERE HotelId=@HId AND DATE(CreatedAt) BETWEEN @From AND @To AND OrderStatus <> 'Cancelled'";
            if (!string.IsNullOrEmpty(orderType)) where += " AND OrderType=@OType";
            return await db.QueryAsync(
                $@"SELECT DATE(CreatedAt) AS Day,
                          COUNT(*) AS OrderCount,
                          COALESCE(SUM(GrandTotal),0) AS Revenue
                   FROM orders {where}
                   GROUP BY DATE(CreatedAt)
                   ORDER BY Day ASC",
                new { HId=hotelId, From=from, To=to, OType=orderType });
        }

        /// <summary>Top-selling catalog items by quantity and revenue.</summary>
        public async Task<IEnumerable<dynamic>> GetTopOrderItems(
            int hotelId, DateTime from, DateTime to, string? category, int topN = 10)
        {
            using var db = GetDb();
            var where = @"WHERE o.HotelId=@HId
                            AND DATE(o.CreatedAt) BETWEEN @From AND @To
                            AND o.OrderStatus <> 'Cancelled'";
            if (!string.IsNullOrEmpty(category)) where += " AND o.Category=@Cat";
            return await db.QueryAsync(
                $@"SELECT oi.ItemName,
                          o.Category,
                          COUNT(DISTINCT o.OrderId)       AS OrderCount,
                          COALESCE(SUM(oi.Quantity),0)    AS TotalQty,
                          COALESCE(SUM(oi.LineTotalWithTax),0) AS Revenue
                   FROM orderitems oi
                   JOIN orders o ON o.OrderId = oi.OrderId
                   {where}
                   GROUP BY oi.ItemName, o.Category
                   ORDER BY Revenue DESC
                   LIMIT {topN}",
                new { HId=hotelId, From=from, To=to, Cat=category });
        }

        // ── BILL ENTRIES ───────────────────────────────────────────────────────

        public async Task<IEnumerable<BillEntry>> GetBookingFolio(int bookingId)
        {
            using var db = GetDb();
            return await db.QueryAsync<BillEntry>(
                "SELECT * FROM vw_bookingfolio WHERE BookingId=@BId ORDER BY PostedAt",
                new { BId = bookingId });
        }

        public async Task<int> PostManualBillEntry(BillEntry entry)
        {
            using var db = GetDb();
            return await db.ExecuteScalarAsync<int>(@"
                INSERT INTO billentries(BookingId,EntryType,Description,ReferenceId,ReferenceType,Amount,TaxAmount,GrandAmount,PostedBy)
                VALUES(@BookingId,@EntryType,@Description,@ReferenceId,@ReferenceType,@Amount,@TaxAmount,@GrandAmount,@PostedBy);
                SELECT LAST_INSERT_ID();", entry);
        }

        public async Task VoidBillEntry(int billEntryId, int userId, string reason)
        {
            using var db = GetDb();
            await db.ExecuteAsync(@"
                UPDATE billentries
                SET IsVoided=1, VoidedAt=NOW(), VoidedBy=@Uid, VoidReason=@Reason
                WHERE BillEntryId=@Id",
                new { Id = billEntryId, Uid = userId, Reason = reason });
        }

        // ── CHECKOUT INVOICE ───────────────────────────────────────────────────

        public async Task<(int InvoiceId, string Message)> GenerateCheckoutInvoice(int bookingId, int userId)
        {
            using var db = GetDb();
            var p = new DynamicParameters();
            p.Add("p_BookingId",  bookingId);
            p.Add("p_UserId",     userId);
            p.Add("p_InvoiceId",  dbType: DbType.Int32,  direction: ParameterDirection.Output);
            p.Add("p_Msg",        dbType: DbType.String, size: 300, direction: ParameterDirection.Output);
            await db.ExecuteAsync("sp_GenerateCheckoutInvoice", p, commandType: CommandType.StoredProcedure);
            return (p.Get<int>("p_InvoiceId"), p.Get<string>("p_Msg") ?? "");
        }

        public async Task<CheckoutInvoice?> GetCheckoutInvoice(int bookingId)
        {
            using var db = GetDb();
            return await db.QueryFirstOrDefaultAsync<CheckoutInvoice>(@"
                SELECT ci.*,
                       b.BookingReference,
                       CONCAT(c.FirstName,' ',c.LastName) AS GuestName,
                       r.RoomNumber
                FROM checkoutinvoices ci
                JOIN bookings  b ON b.BookingId  = ci.BookingId
                JOIN customers c ON c.CustomerId = ci.CustomerId
                LEFT JOIN rooms r ON r.RoomId    = b.RoomId
                WHERE ci.BookingId = @BId",
                new { BId = bookingId });
        }

        // ══════════════════════════════════════════════════════════════════════
        // RECIPE-BASED INVENTORY MANAGEMENT SYSTEM (RIMS)
        // ══════════════════════════════════════════════════════════════════════

        // ── Inventory Items ───────────────────────────────────────────────────

        public async Task<InventoryDashboardStats> GetInventoryDashboard(int hotelId)
        {
            using var db = GetDb();
            var items = (await db.QueryAsync<InventoryItem>(
                "SELECT * FROM inventoryitems WHERE HotelId=@HId AND IsActive=1",
                new { HId = hotelId })).AsList();

            var todayMovements = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM stockmovements WHERE HotelId=@HId AND DATE(CreatedAt)=CURDATE()",
                new { HId = hotelId });

            var totalRecipes = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM recipes WHERE HotelId=@HId AND IsActive=1",
                new { HId = hotelId });

            return new InventoryDashboardStats
            {
                TotalItems      = items.Count,
                LowStockItems   = items.Count(i => i.IsLowStock && i.CurrentStock > 0),
                OutOfStockItems = items.Count(i => i.CurrentStock <= 0),
                TotalRecipes    = totalRecipes,
                TotalStockValue = items.Sum(i => i.StockValue),
                TodayMovements  = todayMovements,
                LowStockList    = items.Where(i => i.IsLowStock).OrderBy(i => i.CurrentStock).Take(10).ToList()
            };
        }

        public async Task<IEnumerable<InventoryItem>> GetInventoryItems(int hotelId, string? category, bool lowStockOnly)
        {
            using var db = GetDb();
            var sql = "SELECT * FROM inventoryitems WHERE HotelId=@HId AND IsActive=1";
            if (!string.IsNullOrEmpty(category)) sql += " AND Category=@Cat";
            if (lowStockOnly) sql += " AND CurrentStock <= MinStockLevel";
            sql += " ORDER BY Category, ItemName";
            return await db.QueryAsync<InventoryItem>(sql, new { HId = hotelId, Cat = category });
        }

        public async Task<InventoryItem?> GetInventoryItemById(int itemId)
        {
            using var db = GetDb();
            return await db.QueryFirstOrDefaultAsync<InventoryItem>(
                "SELECT * FROM inventoryitems WHERE ItemId=@Id", new { Id = itemId });
        }

        public async Task<int> CreateInventoryItem(int hotelId, CreateInventoryItemRequest req)
        {
            using var db = GetDb();
            return await db.ExecuteScalarAsync<int>(@"
                INSERT INTO inventoryitems
                    (HotelId,ItemName,Description,Category,Unit,CurrentStock,MinStockLevel,
                     ReorderQty,CostPerUnit,Supplier,IsActive,CreatedAt,UpdatedAt)
                VALUES
                    (@HotelId,@ItemName,@Description,@Category,@Unit,@CurrentStock,@MinStockLevel,
                     @ReorderQty,@CostPerUnit,@Supplier,1,NOW(),NOW());
                SELECT LAST_INSERT_ID();",
                new
                {
                    HotelId = hotelId, req.ItemName, req.Description, req.Category, req.Unit,
                    req.CurrentStock, req.MinStockLevel, req.ReorderQty, req.CostPerUnit, req.Supplier
                });
        }

        public async Task UpdateInventoryItem(int itemId, CreateInventoryItemRequest req)
        {
            using var db = GetDb();
            await db.ExecuteAsync(@"
                UPDATE inventoryitems SET
                    ItemName=@ItemName, Description=@Description, Category=@Category,
                    Unit=@Unit, MinStockLevel=@MinStockLevel, ReorderQty=@ReorderQty,
                    CostPerUnit=@CostPerUnit, Supplier=@Supplier, UpdatedAt=NOW()
                WHERE ItemId=@Id",
                new
                {
                    Id = itemId, req.ItemName, req.Description, req.Category, req.Unit,
                    req.MinStockLevel, req.ReorderQty, req.CostPerUnit, req.Supplier
                });
        }

        public async Task DeactivateInventoryItem(int itemId)
        {
            using var db = GetDb();
            await db.ExecuteAsync("UPDATE inventoryitems SET IsActive=0,UpdatedAt=NOW() WHERE ItemId=@Id",
                new { Id = itemId });
        }

        // ── Stock Movements ───────────────────────────────────────────────────

        public async Task<IEnumerable<StockMovement>> GetStockMovements(
            int hotelId, int? itemId, string? movementType,
            DateTime? from, DateTime? to, int page, int pageSize)
        {
            using var db = GetDb();
            var sql = @"
                SELECT sm.*, ii.ItemName, ii.Unit, u.FullName AS CreatedByName
                FROM stockmovements sm
                JOIN inventoryitems ii ON ii.ItemId = sm.ItemId
                LEFT JOIN users u ON u.UserId = sm.CreatedBy
                WHERE sm.HotelId=@HId";
            if (itemId.HasValue)                     sql += " AND sm.ItemId=@IId";
            if (!string.IsNullOrEmpty(movementType)) sql += " AND sm.MovementType=@MType";
            if (from.HasValue)                       sql += " AND sm.CreatedAt>=@From";
            if (to.HasValue)                         sql += " AND sm.CreatedAt<DATE_ADD(@To,INTERVAL 1 DAY)";
            sql += $" ORDER BY sm.CreatedAt DESC LIMIT {pageSize} OFFSET {(page - 1) * pageSize}";
            return await db.QueryAsync<StockMovement>(sql,
                new { HId = hotelId, IId = itemId, MType = movementType, From = from, To = to });
        }

        public async Task<(int MovementId, string Message)> AddStockMovement(
            int hotelId, int userId, StockMovementRequest req)
        {
            using var db = GetDb();
            // For OUT / WASTE, verify we have enough stock
            if (req.MovementType is "OUT" or "WASTE")
            {
                var current = await db.ExecuteScalarAsync<decimal>(
                    "SELECT CurrentStock FROM inventoryitems WHERE ItemId=@Id", new { Id = req.ItemId });
                if (current < req.Quantity)
                    return (0, $"ERROR: Insufficient stock. Available: {current}, Requested: {req.Quantity}");
            }

            var costPer  = req.CostPerUnit;
            var totalCost = costPer * req.Quantity;

            // Insert movement
            var movId = await db.ExecuteScalarAsync<int>(@"
                INSERT INTO stockmovements
                    (HotelId,ItemId,MovementType,Quantity,CostPerUnit,TotalCost,
                     ReferenceType,ReferenceId,Notes,CreatedBy,CreatedAt)
                VALUES
                    (@HId,@IId,@MType,@Qty,@CostPer,@TotalCost,
                     @RefType,@RefId,@Notes,@By,NOW());
                SELECT LAST_INSERT_ID();",
                new
                {
                    HId = hotelId, IId = req.ItemId, MType = req.MovementType,
                    Qty = req.Quantity, CostPer = costPer, TotalCost = totalCost,
                    RefType = req.ReferenceType, RefId = req.ReferenceId,
                    Notes = req.Notes, By = userId
                });

            // Update current stock
            var delta = req.MovementType == "IN" ? req.Quantity : -req.Quantity;
            if (req.MovementType == "ADJUSTMENT")
            {
                // ADJUSTMENT sets stock to the quantity value directly
                await db.ExecuteAsync(
                    "UPDATE inventoryitems SET CurrentStock=@Qty,UpdatedAt=NOW() WHERE ItemId=@Id",
                    new { Qty = req.Quantity, Id = req.ItemId });
            }
            else
            {
                await db.ExecuteAsync(
                    "UPDATE inventoryitems SET CurrentStock=CurrentStock+@Delta,UpdatedAt=NOW() WHERE ItemId=@Id",
                    new { Delta = delta, Id = req.ItemId });
            }

            return (movId, $"SUCCESS: Stock {req.MovementType} recorded");
        }

        // ── Recipes ───────────────────────────────────────────────────────────

        /// <summary>
        /// Shared helper — given a recipe ID and its ingredient lines (with current stock),
        /// compute how many full portions can be made right now.
        /// Returns (maxPortions, bottleneckIngredient).
        /// </summary>
        private static (int MaxPortions, string? Bottleneck) CalcMaxPortions(
            IEnumerable<dynamic> ingredientLines)
        {
            int max = int.MaxValue;
            string? bottleneck = null;
            foreach (var ing in ingredientLines)
            {
                decimal reqPerYield = (decimal)ing.RequiredPerYield;
                if (reqPerYield <= 0) continue;
                int canMake = (int)Math.Floor((double)((decimal)ing.CurrentStock / reqPerYield));
                if (canMake < max)
                {
                    max = canMake;
                    bottleneck = (string)ing.ItemName;
                }
            }
            return (max == int.MaxValue ? 0 : max, bottleneck);
        }

        public async Task<IEnumerable<Recipe>> GetRecipes(int hotelId, string? category)
        {
            using var db = GetDb();
            var sql = @"
                SELECT r.*, oc.ItemName AS CatalogItemName,
                       COALESCE((
                           SELECT SUM(ri.Quantity * ii.CostPerUnit)
                           FROM recipeingredients ri
                           JOIN inventoryitems ii ON ii.ItemId = ri.ItemId
                           WHERE ri.RecipeId = r.RecipeId
                       ),0) AS IngredientCost
                FROM recipes r
                LEFT JOIN ordercatalog oc ON oc.CatalogId = r.CatalogId
                WHERE r.HotelId=@HId AND r.IsActive=1";
            if (!string.IsNullOrEmpty(category)) sql += " AND r.Category=@Cat";
            sql += " ORDER BY r.Category, r.RecipeName";
            var recipes = (await db.QueryAsync<Recipe>(sql, new { HId = hotelId, Cat = category })).AsList();

            if (recipes.Any())
            {
                var ids = recipes.Select(r => r.RecipeId).ToList();

                // Load ingredients WITH current stock for MaxPortions calculation
                var allIngredients = (await db.QueryAsync<dynamic>(@"
                    SELECT ri.RecipeId, ri.ItemId, ri.Quantity AS RequiredPerYield,
                           ii.ItemName, ii.Unit, ii.CostPerUnit, ii.CurrentStock
                    FROM recipeingredients ri
                    JOIN inventoryitems ii ON ii.ItemId = ri.ItemId
                    WHERE ri.RecipeId IN @Ids",
                    new { Ids = ids })).AsList();

                foreach (var recipe in recipes)
                {
                    var lines = allIngredients.Where(i => (int)i.RecipeId == recipe.RecipeId).ToList();

                    // Typed ingredient list for the UI
                    recipe.Ingredients = lines.Select(i => new RecipeIngredient
                    {
                        RecipeId    = (int)i.RecipeId,
                        ItemId      = (int)i.ItemId,
                        Quantity    = (decimal)i.RequiredPerYield,
                        ItemName    = (string)i.ItemName,
                        Unit        = (string)i.Unit,
                        CostPerUnit = (decimal)i.CostPerUnit
                    }).ToList();

                    // Compute max portions
                    var (maxP, bottleneck) = CalcMaxPortions(lines);
                    recipe.MaxPortions          = maxP;
                    recipe.BottleneckIngredient = bottleneck;
                    recipe.HasStockShortage     = maxP == 0;
                }
            }
            return recipes;
        }

        public async Task<Recipe?> GetRecipeById(int recipeId)
        {
            using var db = GetDb();
            var recipe = await db.QueryFirstOrDefaultAsync<Recipe>(@"
                SELECT r.*, oc.ItemName AS CatalogItemName,
                       COALESCE((
                           SELECT SUM(ri.Quantity * ii.CostPerUnit)
                           FROM recipeingredients ri
                           JOIN inventoryitems ii ON ii.ItemId = ri.ItemId
                           WHERE ri.RecipeId = r.RecipeId
                       ),0) AS IngredientCost
                FROM recipes r
                LEFT JOIN ordercatalog oc ON oc.CatalogId = r.CatalogId
                WHERE r.RecipeId=@Id",
                new { Id = recipeId });

            if (recipe != null)
            {
                var lines = (await db.QueryAsync<dynamic>(@"
                    SELECT ri.RecipeId, ri.ItemId, ri.Quantity AS RequiredPerYield,
                           ii.ItemName, ii.Unit, ii.CostPerUnit, ii.CurrentStock
                    FROM recipeingredients ri
                    JOIN inventoryitems ii ON ii.ItemId = ri.ItemId
                    WHERE ri.RecipeId=@Id",
                    new { Id = recipeId })).AsList();

                recipe.Ingredients = lines.Select(i => new RecipeIngredient
                {
                    RecipeId    = (int)i.RecipeId,
                    ItemId      = (int)i.ItemId,
                    Quantity    = (decimal)i.RequiredPerYield,
                    ItemName    = (string)i.ItemName,
                    Unit        = (string)i.Unit,
                    CostPerUnit = (decimal)i.CostPerUnit
                }).ToList();

                var (maxP, bottleneck) = CalcMaxPortions(lines);
                recipe.MaxPortions          = maxP;
                recipe.BottleneckIngredient = bottleneck;
                recipe.HasStockShortage     = maxP == 0;
            }
            return recipe;
        }

        public async Task<int> CreateRecipe(int hotelId, CreateRecipeRequest req)
        {
            using var db = GetDb();

            // ── 1. Auto-create a matching ordercatalog entry ──────────────────
            var catalogId = await db.ExecuteScalarAsync<int>(@"
                INSERT INTO ordercatalog
                    (HotelId,Category,ItemName,Description,UnitPrice,Unit,TaxPercent,IsAvailable,SortOrder)
                VALUES
                    (@HotelId,@Category,@ItemName,@Description,@UnitPrice,@Unit,@TaxPercent,1,0);
                SELECT LAST_INSERT_ID();",
                new
                {
                    HotelId     = hotelId,
                    Category    = req.Category,
                    ItemName    = req.RecipeName,
                    Description = req.Description,
                    UnitPrice   = req.SellingPrice,
                    Unit        = req.Unit,
                    TaxPercent  = req.TaxPercent
                });

            // ── 2. Insert the recipe row linked to the catalog entry ──────────
            var recipeId = await db.ExecuteScalarAsync<int>(@"
                INSERT INTO recipes
                    (HotelId,CatalogId,RecipeName,Description,Category,Yield,
                     SellingPrice,Unit,TaxPercent,Instructions,IsActive,CreatedAt,UpdatedAt)
                VALUES
                    (@HotelId,@CatalogId,@RecipeName,@Description,@Category,@Yield,
                     @SellingPrice,@Unit,@TaxPercent,@Instructions,1,NOW(),NOW());
                SELECT LAST_INSERT_ID();",
                new
                {
                    HotelId      = hotelId,
                    CatalogId    = catalogId,
                    req.RecipeName, req.Description,
                    req.Category,   req.Yield,
                    req.SellingPrice, req.Unit, req.TaxPercent,
                    req.Instructions
                });

            // ── 3. Insert ingredients ─────────────────────────────────────────
            foreach (var ing in req.Ingredients)
            {
                await db.ExecuteAsync(@"
                    INSERT INTO recipeingredients(RecipeId,ItemId,Quantity,Notes)
                    VALUES(@RecipeId,@ItemId,@Quantity,@Notes)",
                    new { RecipeId = recipeId, ing.ItemId, ing.Quantity, ing.Notes });
            }
            return recipeId;
        }

        public async Task UpdateRecipe(int recipeId, CreateRecipeRequest req)
        {
            using var db = GetDb();

            // Get current recipe to find its catalogId
            var existing = await db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT CatalogId, HotelId FROM recipes WHERE RecipeId=@Id", new { Id = recipeId });

            int? catalogId = existing?.CatalogId;
            int  hotelId   = (int)(existing?.HotelId ?? 1);

            if (catalogId.HasValue)
            {
                // ── Sync ordercatalog entry ────────────────────────────────────
                await db.ExecuteAsync(@"
                    UPDATE ordercatalog SET
                        Category=@Category, ItemName=@ItemName, Description=@Description,
                        UnitPrice=@UnitPrice, Unit=@Unit, TaxPercent=@TaxPercent
                    WHERE CatalogId=@CatalogId",
                    new
                    {
                        CatalogId   = catalogId.Value,
                        Category    = req.Category,
                        ItemName    = req.RecipeName,
                        Description = req.Description,
                        UnitPrice   = req.SellingPrice,
                        Unit        = req.Unit,
                        TaxPercent  = req.TaxPercent
                    });
            }
            else
            {
                // Recipe had no catalog entry yet — create one now
                catalogId = await db.ExecuteScalarAsync<int>(@"
                    INSERT INTO ordercatalog
                        (HotelId,Category,ItemName,Description,UnitPrice,Unit,TaxPercent,IsAvailable,SortOrder)
                    VALUES
                        (@HotelId,@Category,@ItemName,@Description,@UnitPrice,@Unit,@TaxPercent,1,0);
                    SELECT LAST_INSERT_ID();",
                    new
                    {
                        HotelId     = hotelId,
                        Category    = req.Category,
                        ItemName    = req.RecipeName,
                        Description = req.Description,
                        UnitPrice   = req.SellingPrice,
                        Unit        = req.Unit,
                        TaxPercent  = req.TaxPercent
                    });
            }

            // ── Update recipe row ──────────────────────────────────────────────
            await db.ExecuteAsync(@"
                UPDATE recipes SET
                    CatalogId=@CatalogId, RecipeName=@RecipeName, Description=@Description,
                    Category=@Category, Yield=@Yield, SellingPrice=@SellingPrice,
                    Unit=@Unit, TaxPercent=@TaxPercent,
                    Instructions=@Instructions, UpdatedAt=NOW()
                WHERE RecipeId=@Id",
                new
                {
                    Id = recipeId, CatalogId = catalogId,
                    req.RecipeName, req.Description,
                    req.Category,   req.Yield,
                    req.SellingPrice, req.Unit, req.TaxPercent,
                    req.Instructions
                });

            // ── Replace ingredients ───────────────────────────────────────────
            await db.ExecuteAsync("DELETE FROM recipeingredients WHERE RecipeId=@Id", new { Id = recipeId });
            foreach (var ing in req.Ingredients)
            {
                await db.ExecuteAsync(@"
                    INSERT INTO recipeingredients(RecipeId,ItemId,Quantity,Notes)
                    VALUES(@RecipeId,@ItemId,@Quantity,@Notes)",
                    new { RecipeId = recipeId, ing.ItemId, ing.Quantity, ing.Notes });
            }
        }

        public async Task DeactivateRecipe(int recipeId)
        {
            using var db = GetDb();
            // Also hide from ordercatalog so it stops appearing in order picker
            var catalogId = await db.ExecuteScalarAsync<int?>(
                "SELECT CatalogId FROM recipes WHERE RecipeId=@Id", new { Id = recipeId });
            if (catalogId.HasValue)
                await db.ExecuteAsync("UPDATE ordercatalog SET IsAvailable=0 WHERE CatalogId=@CId",
                    new { CId = catalogId.Value });
            await db.ExecuteAsync("UPDATE recipes SET IsActive=0,UpdatedAt=NOW() WHERE RecipeId=@Id",
                new { Id = recipeId });
        }

        /// <summary>Check if stock is sufficient for N portions — returns full per-ingredient breakdown
        /// including MaxPortionsPossible so the UI can show "you can make X more".</summary>
        public async Task<RecipeStockCheckResult> CheckRecipeStock(int recipeId, int portions)
        {
            using var db = GetDb();

            var recipeName = await db.ExecuteScalarAsync<string>(
                "SELECT RecipeName FROM recipes WHERE RecipeId=@Id", new { Id = recipeId }) ?? "";

            var rawLines = (await db.QueryAsync<dynamic>(@"
                SELECT ri.ItemId, ri.Quantity AS RequiredPerYield, ii.ItemName, ii.Unit,
                       ii.CurrentStock,
                       ri.Quantity * @P AS RequiredTotal
                FROM recipeingredients ri
                JOIN inventoryitems ii ON ii.ItemId = ri.ItemId
                WHERE ri.RecipeId=@Id",
                new { Id = recipeId, P = portions })).AsList();

            var lines = rawLines.Select(l =>
            {
                decimal reqPerYield  = (decimal)l.RequiredPerYield;
                decimal currentStock = (decimal)l.CurrentStock;
                decimal reqTotal     = (decimal)l.RequiredTotal;
                int     maxFromThis  = reqPerYield > 0
                    ? (int)Math.Floor((double)(currentStock / reqPerYield))
                    : int.MaxValue;
                return new RecipeStockLine
                {
                    ItemId            = (int)l.ItemId,
                    ItemName          = (string)l.ItemName,
                    Unit              = (string)l.Unit,
                    RequiredPerYield  = reqPerYield,
                    RequiredTotal     = reqTotal,
                    CurrentStock      = currentStock,
                    HasStock          = currentStock >= reqTotal,
                    MaxPortionsFromThis = maxFromThis == int.MaxValue ? 9999 : maxFromThis
                };
            }).ToList();

            int maxPossible = lines.Count > 0
                ? lines.Min(l => l.MaxPortionsFromThis)
                : 0;
            bool canMake   = lines.All(l => l.HasStock);
            string? bottleneck = lines.Count > 0
                ? lines.OrderBy(l => l.MaxPortionsFromThis).First().ItemName
                : null;

            return new RecipeStockCheckResult
            {
                RecipeId             = recipeId,
                RecipeName           = recipeName,
                RequestedPortions    = portions,
                CanMake              = canMake,
                MaxPortionsPossible  = maxPossible,
                BottleneckIngredient = bottleneck,
                Lines                = lines
            };
        }

        /// <summary>Deduct inventory for N portions of a recipe (called on order Delivered).</summary>
        public async Task<(bool Ok, string Message)> DeductRecipeStock(
            int hotelId, int userId, DeductRecipeStockRequest req)
        {
            using var db = GetDb();

            var recipe = await db.QueryFirstOrDefaultAsync<Recipe>(
                "SELECT * FROM recipes WHERE RecipeId=@Id AND IsActive=1", new { Id = req.RecipeId });
            if (recipe == null) return (false, "Recipe not found");

            var ingredients = (await db.QueryAsync<dynamic>(@"
                SELECT ri.ItemId, ri.Quantity * @P AS TotalQty, ii.ItemName, ii.Unit,
                       ii.CurrentStock, ii.CostPerUnit
                FROM recipeingredients ri
                JOIN inventoryitems ii ON ii.ItemId = ri.ItemId
                WHERE ri.RecipeId=@Id",
                new { Id = req.RecipeId, P = req.Portions })).AsList();

            // Validate stock for all ingredients first
            foreach (var ing in ingredients)
            {
                if ((decimal)ing.CurrentStock < (decimal)ing.TotalQty)
                    return (false,
                        $"Insufficient stock for '{ing.ItemName}'. " +
                        $"Available: {ing.CurrentStock} {ing.Unit}, Required: {ing.TotalQty} {ing.Unit}");
            }

            // Deduct each ingredient
            foreach (var ing in ingredients)
            {
                await db.ExecuteAsync(@"
                    INSERT INTO stockmovements
                        (HotelId,ItemId,MovementType,Quantity,CostPerUnit,TotalCost,
                         ReferenceType,ReferenceId,Notes,CreatedBy,CreatedAt)
                    VALUES
                        (@HId,@IId,'OUT',@Qty,@CostPer,@Total,
                         'Order',@RefId,@Notes,@By,NOW())",
                    new
                    {
                        HId = hotelId, IId = (int)ing.ItemId,
                        Qty = (decimal)ing.TotalQty,
                        CostPer = (decimal)ing.CostPerUnit,
                        Total = (decimal)ing.TotalQty * (decimal)ing.CostPerUnit,
                        RefId = req.OrderId,
                        Notes = req.Notes ?? $"Recipe: {recipe.RecipeName} x{req.Portions}",
                        By = userId
                    });

                await db.ExecuteAsync(
                    "UPDATE inventoryitems SET CurrentStock=CurrentStock-@Qty,UpdatedAt=NOW() WHERE ItemId=@Id",
                    new { Qty = (decimal)ing.TotalQty, Id = (int)ing.ItemId });
            }

            return (true, $"Stock deducted for {recipe.RecipeName} x{req.Portions} portion(s)");
        }
    }
}
