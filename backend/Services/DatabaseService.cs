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
        public async Task<IEnumerable<RoomAvailability>> GetAvailability(int? rtId, DateTime from, DateTime to)
        {
            using var db = GetDb();
            var sql = @"SELECT ra.*,rt.TypeName FROM roomavailability ra
                        JOIN roomtypes rt ON rt.RoomTypeId=ra.RoomTypeId
                        WHERE ra.AvailDate BETWEEN @From AND @To";
            if (rtId.HasValue) sql += " AND ra.RoomTypeId=@RtId";
            sql += " ORDER BY ra.AvailDate,rt.SortOrder";
            return await db.QueryAsync<RoomAvailability>(sql, new { From=from, To=to, RtId=rtId });
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
            decimal grandTotal   = subTotal + taxAmount;
            decimal commAmount   = Math.Round(grandTotal * commPct / 100m, 2);
            decimal netToHotel   = grandTotal - commAmount;

            // 7. Generate booking reference and insert
            var bookingRef = $"BK{DateTime.UtcNow:yyyyMMddHHmmss}{req.RoomTypeId:D2}";
            var bookingId  = await db.ExecuteScalarAsync<int>(@"
                INSERT INTO bookings
                  (HotelId,RoomTypeId,CustomerId,PartnerId,BookingReference,
                   CheckInDate,CheckOutDate,AdultsCount,ChildrenCount,
                   RoomRate,SubTotal,TaxAmount,DiscountAmount,GrandTotal,
                   CommissionAmount,NetToHotel,PaymentMode,BookingStatus,BookingSource,
                   SpecialRequests,ConfirmedAt,CreatedAt)
                VALUES
                  (@HId,@RtId,@CId,@PId,@Ref,
                   @CIn,@COut,@Adults,@Children,
                   @RoomRate,@Sub,@Tax,0,@Grand,
                   @Comm,@Net,@PayMode,'Confirmed',@Source,
                   @SpecReqs,NOW(),NOW());
                SELECT LAST_INSERT_ID();",
                new {
                    HId=req.HotelId, RtId=req.RoomTypeId, CId=custId,
                    PId=req.PartnerId.HasValue && req.PartnerId>0 ? (object)req.PartnerId.Value : DBNull.Value,
                    Ref=bookingRef, CIn=checkIn, COut=checkOut,
                    Adults=req.AdultsCount, Children=req.ChildrenCount,
                    RoomRate=rateOverridden ? req.OverrideRoomRate!.Value : (nights>0 ? subTotal/nights : subTotal),
                    Sub=subTotal, Tax=taxAmount, Grand=grandTotal,
                    Comm=commAmount, Net=netToHotel,
                    PayMode=req.PaymentMode ?? "PayAtHotel",
                    Source=req.BookingSource ?? "Website",
                    SpecReqs=req.SpecialRequests
                });

            if (bookingId == 0) return (0, "", "ERROR: Booking insert failed");

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

        public async Task<Booking?> GetBooking(int id)
        {
            using var db = GetDb();
            return await db.QueryFirstOrDefaultAsync<Booking>("SELECT * FROM vw_BookingDetails WHERE BookingId=@Id", new { Id = id });
        }

        public async Task<Booking?> GetBookingByRef(string refNo)
        {
            using var db = GetDb();
            return await db.QueryFirstOrDefaultAsync<Booking>("SELECT * FROM vw_BookingDetails WHERE BookingReference=@Ref", new { Ref = refNo });
        }

        // ── CUSTOMER PORTAL — all bookings for a given guest email ─────────
        public async Task<IEnumerable<Booking>> GetBookingsByEmail(string email)
        {
            using var db = GetDb();
            return await db.QueryAsync<Booking>(
                "SELECT * FROM vw_BookingDetails WHERE GuestEmail=@Email ORDER BY BookingDate DESC",
                new { Email = email });
        }

        public async Task<(IEnumerable<Booking> items, int total)> GetBookings(ReportFilter f)
        {
            using var db = GetDb();
            var where = "WHERE BookingDate BETWEEN @From AND @To";
            if (f.PartnerId.HasValue)               where += " AND PartnerId=@PId";
            if (!string.IsNullOrEmpty(f.Status))    where += " AND BookingStatus=@Status";
            if (!string.IsNullOrEmpty(f.BookingSource)) where += " AND BookingSource=@Source";
            var total = await db.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM vw_BookingDetails {where}",
                new { From=f.FromDate, To=f.ToDate, PId=f.PartnerId, Status=f.Status, Source=f.BookingSource });
            var items = await db.QueryAsync<Booking>(
                $"SELECT * FROM vw_BookingDetails {where} ORDER BY BookingDate DESC LIMIT {f.PageSize} OFFSET {(f.Page-1)*f.PageSize}",
                new { From=f.FromDate, To=f.ToDate, PId=f.PartnerId, Status=f.Status, Source=f.BookingSource });
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
            quote.GrandTotal=subTotal+quote.TaxAmount;
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
            try { stats.ChannelSummary=(await db.QueryAsync<ChannelRevenueSummary>("SELECT * FROM vw_ChannelRevenueSummary")).AsList(); } catch { stats.ChannelSummary=new(); }
            try { stats.RoomOccupancy=(await db.QueryAsync<RoomTypeOccupancy>(@"SELECT rt.TypeName,ra.TotalRooms,ra.BlockedRooms,ra.BookedRooms,(ra.TotalRooms-ra.BlockedRooms-ra.BookedRooms) AS AvailableRooms,CASE WHEN ra.TotalRooms>0 THEN ROUND(ra.BookedRooms*100.0/ra.TotalRooms,1) ELSE 0 END AS OccupancyPct FROM roomavailability ra JOIN roomtypes rt ON rt.RoomTypeId=ra.RoomTypeId WHERE ra.AvailDate=@D AND rt.HotelId=@H ORDER BY rt.SortOrder",new{D=today,H=hotelId})).AsList(); } catch { stats.RoomOccupancy=new(); }
            try { stats.RecentBookings=(await db.QueryAsync<RecentBooking>("SELECT BookingId,BookingReference,GuestName,RoomTypeName,CheckInDate,GrandTotal,BookingStatus,ChannelName,BookingDate AS CreatedAt FROM vw_BookingDetails ORDER BY BookingDate DESC LIMIT 10")).AsList(); } catch { stats.RecentBookings=new(); }
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
            int hotelId, int bookingId, int roomId, int customerId,
            string category, string priority, string? specInstr, DateTime? deliveryTime, int createdBy)
        {
            using var db = GetDb();
            var p = new DynamicParameters();
            p.Add("p_HotelId",      hotelId);
            p.Add("p_BookingId",    bookingId);
            p.Add("p_RoomId",       roomId);
            p.Add("p_CustomerId",   customerId);
            p.Add("p_Category",     category);
            p.Add("p_Priority",     priority);
            p.Add("p_SpecInstr",    specInstr);
            p.Add("p_DeliveryTime", deliveryTime);
            p.Add("p_CreatedBy",    createdBy);
            p.Add("p_OrderId",     dbType: DbType.Int32,  direction: ParameterDirection.Output);
            p.Add("p_OrderNumber", dbType: DbType.String, size: 30,  direction: ParameterDirection.Output);
            p.Add("p_Msg",         dbType: DbType.String, size: 300, direction: ParameterDirection.Output);
            await db.ExecuteAsync("sp_CreateOrder", p, commandType: CommandType.StoredProcedure);
            return (p.Get<int>("p_OrderId"), p.Get<string>("p_OrderNumber") ?? "", p.Get<string>("p_Msg") ?? "");
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
            await db.ExecuteAsync("CALL sp_RecalcOrder(@Id)", new { Id = orderId });
        }

        public async Task RemoveOrderItem(int orderItemId)
        {
            using var db = GetDb();
            var orderId = await db.ExecuteScalarAsync<int>(
                "SELECT OrderId FROM orderitems WHERE OrderItemId=@Id", new { Id = orderItemId });
            await db.ExecuteAsync("DELETE FROM orderitems WHERE OrderItemId=@Id", new { Id = orderItemId });
            if (orderId > 0) await db.ExecuteAsync("CALL sp_RecalcOrder(@Id)", new { Id = orderId });
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
            int? bookingId = null, DateTime? from = null, DateTime? to = null,
            int page = 1, int pageSize = 50)
        {
            using var db = GetDb();
            var sql = @"SELECT od.*, b.HotelId
                        FROM vw_OrderDetails od
                        JOIN bookings b ON b.BookingId = od.BookingId
                        WHERE b.HotelId = @HId";
            if (!string.IsNullOrEmpty(status))   sql += " AND od.OrderStatus=@Status";
            if (!string.IsNullOrEmpty(category)) sql += " AND od.Category=@Cat";
            if (bookingId.HasValue)              sql += " AND od.BookingId=@BId";
            if (from.HasValue)                   sql += " AND DATE(od.OrderDate)>=@From";
            if (to.HasValue)                     sql += " AND DATE(od.OrderDate)<=@To";
            sql += $" ORDER BY od.OrderDate DESC LIMIT {pageSize} OFFSET {(page - 1) * pageSize}";
            return await db.QueryAsync<Order>(sql,
                new { HId = hotelId, Status = status, Cat = category, BId = bookingId, From = from, To = to });
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
            var p = new DynamicParameters();
            p.Add("p_OrderId",   orderId);
            p.Add("p_NewStatus", newStatus);
            p.Add("p_UserId",    userId);
            p.Add("p_Notes",     notes);
            p.Add("p_Msg",       dbType: DbType.String, size: 300, direction: ParameterDirection.Output);
            await db.ExecuteAsync("sp_UpdateOrderStatus", p, commandType: CommandType.StoredProcedure);
            return p.Get<string>("p_Msg") ?? "";
        }

        public async Task<(int BillEntryId, string Message)> BillOrder(int orderId, int postedBy)
        {
            using var db = GetDb();
            var p = new DynamicParameters();
            p.Add("p_OrderId",  orderId);
            p.Add("p_PostedBy", postedBy);
            p.Add("p_BillId",   dbType: DbType.Int32,  direction: ParameterDirection.Output);
            p.Add("p_Msg",      dbType: DbType.String, size: 300, direction: ParameterDirection.Output);
            await db.ExecuteAsync("sp_BillOrder", p, commandType: CommandType.StoredProcedure);
            return (p.Get<int>("p_BillId"), p.Get<string>("p_Msg") ?? "");
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
                JOIN bookings b ON b.BookingId = o.BookingId AND b.HotelId = @HId
                WHERE DATE(o.CreatedAt) BETWEEN @From AND @To",
                new { HId = hotelId, From = from, To = to }) ?? new OrderSummaryStats();
        }

        // ── BILL ENTRIES ───────────────────────────────────────────────────────

        public async Task<IEnumerable<BillEntry>> GetBookingFolio(int bookingId)
        {
            using var db = GetDb();
            return await db.QueryAsync<BillEntry>(
                "SELECT * FROM vw_BookingFolio WHERE BookingId=@BId ORDER BY PostedAt",
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
    }
}
