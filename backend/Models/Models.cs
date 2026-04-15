using System;
using System.Collections.Generic;

namespace HotelChannelManager.Models
{
    public class Hotel
    {
        public int HotelId { get; set; }
        public string HotelName { get; set; } = "";
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? ZipCode { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public int StarRating { get; set; } = 3;
        public TimeSpan CheckInTime { get; set; }
        public TimeSpan CheckOutTime { get; set; }
        public int CancellationPolicyHours { get; set; } = 24;
        public decimal LateCancelChargePercent { get; set; } = 50;
        public decimal TaxPercent { get; set; } = 12;
        public string CurrencyCode { get; set; } = "INR";
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class RoomType
    {
        public int RoomTypeId { get; set; }
        public int HotelId { get; set; }
        public string TypeName { get; set; } = "";
        public string? Description { get; set; }
        public int MaxOccupancy { get; set; } = 2;
        public string? BedType { get; set; }
        public decimal? SizeInSqFt { get; set; }
        public string? ViewType { get; set; }
        public string? Amenities { get; set; }
        public string? ImageUrls { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class Room
    {
        public int RoomId { get; set; }
        public int HotelId { get; set; }
        public int RoomTypeId { get; set; }
        public string RoomNumber { get; set; } = "";
        public int? Floor { get; set; }
        public string Status { get; set; } = "Available";
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public string? TypeName { get; set; } // joined
    }

    public class DefaultRoomRate
    {
        public int DefaultRateId { get; set; }
        public int RoomTypeId { get; set; }
        public decimal WeekdayRate { get; set; }
        public decimal WeekendRate { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? TypeName { get; set; } // joined
    }

    public class RoomRate
    {
        public int RateId { get; set; }
        public int RoomTypeId { get; set; }
        public DateTime RateDate { get; set; }
        public decimal BaseRate { get; set; }
        public decimal? SpecialRate { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int MinNights { get; set; } = 1;
        public string? Notes { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class RoomAvailability
    {
        public int AvailId { get; set; }
        public int RoomTypeId { get; set; }
        public DateTime AvailDate { get; set; }
        public int TotalRooms { get; set; }
        public int BlockedRooms { get; set; }
        public int BookedRooms { get; set; }
        public int AvailableRooms => TotalRooms - BlockedRooms - BookedRooms;
        public string? TypeName { get; set; } // joined
    }

    public class ChannelPartner
    {
        public int PartnerId { get; set; }
        public int HotelId { get; set; }
        public string PartnerName { get; set; } = "";
        public string PartnerCode { get; set; } = "";
        public string PartnerType { get; set; } = "OTA";
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? APIKey { get; set; }
        public string? APISecret { get; set; }
        public string? WebhookURL { get; set; }
        public string? WebhookSecret { get; set; }
        public decimal CommissionPercent { get; set; }
        public string PaymentMode { get; set; } = "OnlineCollect";
        public int RemittanceDays { get; set; } = 30;
        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ChannelRateMapping
    {
        public int MappingId { get; set; }
        public int PartnerId { get; set; }
        public int RoomTypeId { get; set; }
        public decimal MarkupPercent { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime UpdatedAt { get; set; }
        public string? PartnerName { get; set; }
        public string? TypeName { get; set; }
    }

    public class Customer
    {
        public int CustomerId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? AlternatePhone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? ZipCode { get; set; }
        public string? IDType { get; set; }
        public string? IDNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? Gender { get; set; }
        public string VIPStatus { get; set; } = "Regular";
        public int TotalStays { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }

    public class Booking
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = "";
        public int HotelId { get; set; }
        public int RoomTypeId { get; set; }
        public int? RoomId { get; set; }
        public int CustomerId { get; set; }
        public int? PartnerId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int TotalNights { get; set; }
        public int AdultsCount { get; set; } = 1;
        public int ChildrenCount { get; set; }
        public decimal RoomRate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AddonTotal { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal? NetToHotel { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }
        public string PaymentMode { get; set; } = "PayAtHotel";
        public string BookingStatus { get; set; } = "Confirmed";
        public string BookingSource { get; set; } = "HotelDesk";
        public string? SpecialRequests { get; set; }
        public string? InternalNotes { get; set; }
        public string? CancellationReason { get; set; }
        public decimal CancellationCharge { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public DateTime? CheckedOutAt { get; set; }
        public DateTime ConfirmedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        // Joined fields from view
        public string? HotelName { get; set; }
        public string? CurrencyCode { get; set; }
        public string? RoomTypeName { get; set; }
        public string? RoomNumber { get; set; }
        public string? GuestName { get; set; }
        public string? GuestEmail { get; set; }
        public string? GuestPhone { get; set; }
        public string? Nationality { get; set; }
        public string? IDType { get; set; }
        public string? IDNumber { get; set; }
        public string? VIPStatus { get; set; }
        public string? ChannelName { get; set; }
        public string? PartnerCode { get; set; }
    }

    // ── BOOKING ADD-ONS ───────────────────────────────────────────────────
    public class BookingAddonCatalog
    {
        public int AddonId { get; set; }
        public int HotelId { get; set; }
        public string AddonName { get; set; } = "";
        public string? Description { get; set; }
        public string Category { get; set; } = "Other";   // Meal|Bed|Transfer|Activity|Other
        public string ChargeType { get; set; } = "PerStay"; // PerNight|PerStay|PerPerson|PerPersonPerNight
        public decimal UnitPrice { get; set; }
        public decimal TaxPercent { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BookingAddonItem
    {
        public int ItemId { get; set; }
        public int BookingId { get; set; }
        public int? AddonId { get; set; }
        public string AddonName { get; set; } = "";
        public string ChargeType { get; set; } = "PerStay";
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Payment
    {
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; } = "Full";
        public string PaymentMethod { get; set; } = "Cash";
        public string? TransactionRef { get; set; }
        public string? GatewayName { get; set; }
        public string? GatewayTxnId { get; set; }
        public string Status { get; set; } = "Completed";
        public string? Notes { get; set; }
        public int? ProcessedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? BookingReference { get; set; }
        public string? GuestName { get; set; }
    }

    public class PartnerRemittance
    {
        public int RemittanceId { get; set; }
        public int PartnerId { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public int TotalBookings { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public string Status { get; set; } = "Expected";
        public DateTime? ExpectedDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string? TransactionRef { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PartnerName { get; set; }
    }

    public class User
    {
        public int UserId { get; set; }
        public int? HotelId { get; set; }
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Role { get; set; } = "FrontDesk";
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public int LoginAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }
        public bool MustChangePass { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Dashboard & Reports
    public class DashboardStats
    {
        public int TodayArrivals { get; set; }
        public int TodayDepartures { get; set; }
        public int CurrentlyOccupied { get; set; }
        public int TotalAvailableRooms { get; set; }
        public int TotalRooms { get; set; }
        public decimal OccupancyRate { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal YearRevenue { get; set; }
        public int PendingCheckouts { get; set; }
        public int TotalBookingsThisMonth { get; set; }
        public List<ChannelRevenueSummary> ChannelSummary { get; set; } = new();
        public List<RoomTypeOccupancy> RoomOccupancy { get; set; } = new();
        public List<RecentBooking> RecentBookings { get; set; } = new();
    }

    public class ChannelRevenueSummary
    {
        public string ChannelName { get; set; } = "";
        public string PaymentMode { get; set; } = "";
        public int TotalBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal AvgValue { get; set; }
    }

    public class RoomTypeOccupancy
    {
        public string TypeName { get; set; } = "";
        public int TotalRooms { get; set; }
        public int BlockedRooms { get; set; }
        public int BookedRooms { get; set; }
        public int AvailableRooms { get; set; }
        public decimal OccupancyPct { get; set; }
    }

    public class RecentBooking
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = "";
        public string GuestName { get; set; } = "";
        public string RoomTypeName { get; set; } = "";
        public DateTime CheckInDate { get; set; }
        public decimal GrandTotal { get; set; }
        public string BookingStatus { get; set; } = "";
        public string ChannelName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class SystemSetting
    {
        public string SettingKey { get; set; } = "";
        public string? SettingValue { get; set; }
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ── ORDER MANAGEMENT SYSTEM MODELS ────────────────────────────────────────

    public class OrderCatalogItem
    {
        public int CatalogId { get; set; }
        public int HotelId { get; set; }
        public string Category { get; set; } = "";
        public string ItemName { get; set; } = "";
        public string? Description { get; set; }
        public decimal UnitPrice { get; set; }
        public string Unit { get; set; } = "per item";
        public decimal TaxPercent { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? ImageUrl { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Order
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = "";
        public int HotelId { get; set; }
        // InRoom = linked to a hotel room booking | DirectSale = restaurant/bar walk-in
        public string OrderType { get; set; } = "InRoom";
        public int? BookingId { get; set; }          // nullable — null for DirectSale
        public int? RoomId { get; set; }             // nullable — null for DirectSale
        public int? CustomerId { get; set; }         // nullable — null for anonymous walk-ins
        // Walk-in guest fields (used when OrderType = DirectSale)
        public string? WalkInGuestName { get; set; }
        public string? WalkInGuestPhone { get; set; }
        public string? DirectReceiptNo { get; set; } // receipt number for DirectSale billing
        public string Category { get; set; } = "";
        public string OrderStatus { get; set; } = "Pending";
        public string Priority { get; set; } = "Normal";
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public int? BillEntryId { get; set; }
        public string? SpecialInstructions { get; set; }
        public string? InternalNotes { get; set; }
        public DateTime? DeliveryTime { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public int? CancelledBy { get; set; }
        public string? CancellationReason { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        // Joined via vw_OrderDetails
        public string? BookingReference { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public string? RoomNumber { get; set; }
        public string? RoomTypeName { get; set; }
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        public string? GuestEmail { get; set; }
        public string? VIPStatus { get; set; }
        public string? CreatedByName { get; set; }
        public bool IsBilled { get; set; }
        // Alias used by view (OrderDate maps to CreatedAt)
        public DateTime? OrderDate { get; set; }
    }

    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int? CatalogId { get; set; }
        public string ItemName { get; set; } = "";
        public string? Description { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal TaxAmount { get; set; }       // computed column
        public decimal LineTotal { get; set; }        // computed column
        public decimal LineTotalWithTax { get; set; } // computed column
        public string? Notes { get; set; }
    }

    public class OrderStatusHistory
    {
        public int HistoryId { get; set; }
        public int OrderId { get; set; }
        public string? OldStatus { get; set; }
        public string NewStatus { get; set; } = "";
        public int? ChangedBy { get; set; }
        public string? Notes { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? ChangedByName { get; set; }
    }

    public class BillEntry
    {
        public int BillEntryId { get; set; }
        public int BookingId { get; set; }
        public string EntryType { get; set; } = "";
        public string Description { get; set; } = "";
        public int? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandAmount { get; set; }
        public DateTime PostedAt { get; set; }
        public int? PostedBy { get; set; }
        public bool IsVoided { get; set; }
        public DateTime? VoidedAt { get; set; }
        public int? VoidedBy { get; set; }
        public string? VoidReason { get; set; }
        // Joined
        public string? BookingReference { get; set; }
        public string? GuestName { get; set; }
        public string? RoomNumber { get; set; }
        public string? PostedByName { get; set; }
    }

    public class CheckoutInvoice
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public int BookingId { get; set; }
        public int HotelId { get; set; }
        public int CustomerId { get; set; }
        public decimal RoomCharges { get; set; }
        public decimal ServiceCharges { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime? IssuedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        // Joined
        public string? BookingReference { get; set; }
        public string? GuestName { get; set; }
        public string? RoomNumber { get; set; }
    }

    public class OrderSummaryStats
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int InProgressOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int BilledOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal UnbilledAmount { get; set; }
    }

    // ── RECIPE-BASED INVENTORY MANAGEMENT SYSTEM (RIMS) ──────────────────────

    /// <summary>Master inventory item (ingredient / supply).</summary>
    public class InventoryItem
    {
        public int ItemId { get; set; }
        public int HotelId { get; set; }
        public string ItemName { get; set; } = "";
        public string? Description { get; set; }
        public string Category { get; set; } = "Ingredient"; // Ingredient|Beverage|Packaging|Cleaning|Other
        public string Unit { get; set; } = "kg";             // kg|g|L|ml|pcs|dozen|box
        public decimal CurrentStock { get; set; }
        public decimal MinStockLevel { get; set; }           // low-stock threshold
        public decimal ReorderQty { get; set; }
        public decimal CostPerUnit { get; set; }
        public string? Supplier { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // Computed
        public bool IsLowStock => CurrentStock <= MinStockLevel;
        public decimal StockValue => CurrentStock * CostPerUnit;
    }

    /// <summary>Recipe header linked to an OrderCatalog item.</summary>
    public class Recipe
    {
        public int RecipeId { get; set; }
        public int HotelId { get; set; }
        public int? CatalogId { get; set; }          // links to ordercatalog (auto-managed)
        public string RecipeName { get; set; } = "";
        public string? Description { get; set; }
        public string Category { get; set; } = "";   // mirrors ordercatalog.Category
        public int Yield { get; set; } = 1;          // how many portions this recipe makes
        public string? Instructions { get; set; }
        // Selling price — synced to ordercatalog automatically
        public decimal SellingPrice { get; set; }
        public string Unit { get; set; } = "per portion";
        public decimal TaxPercent { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // Joined
        public string? CatalogItemName { get; set; }
        public decimal IngredientCost { get; set; }  // computed from ingredients
        // ── Stock intelligence (computed by GetRecipes) ───────────────────────
        public int MaxPortions { get; set; }         // max portions makeable right now
        public bool HasStockShortage { get; set; }   // any ingredient below required level
        public string? BottleneckIngredient { get; set; } // ingredient limiting portions
        public List<RecipeIngredient> Ingredients { get; set; } = new();
    }

    /// <summary>Per-ingredient stock line returned by stock-check endpoint.</summary>
    public class RecipeStockLine
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal RequiredPerYield { get; set; }
        public decimal RequiredTotal { get; set; }
        public decimal CurrentStock { get; set; }
        public bool HasStock { get; set; }
        public int MaxPortionsFromThis { get; set; } // floor(CurrentStock / RequiredPerYield)
    }

    /// <summary>Full stock-check result for a recipe.</summary>
    public class RecipeStockCheckResult
    {
        public int RecipeId { get; set; }
        public string RecipeName { get; set; } = "";
        public int RequestedPortions { get; set; }
        public bool CanMake { get; set; }
        public int MaxPortionsPossible { get; set; }  // min across all ingredients
        public string? BottleneckIngredient { get; set; }
        public List<RecipeStockLine> Lines { get; set; } = new();
    }

    /// <summary>One ingredient line inside a recipe.</summary>
    public class RecipeIngredient
    {
        public int LineId { get; set; }
        public int RecipeId { get; set; }
        public int ItemId { get; set; }
        public decimal Quantity { get; set; }        // quantity per recipe yield
        public string? Notes { get; set; }
        // Joined
        public string? ItemName { get; set; }
        public string? Unit { get; set; }
        public decimal CostPerUnit { get; set; }
        public decimal LineCost => Quantity * CostPerUnit;
    }

    /// <summary>Stock movement log (IN / OUT / ADJUSTMENT / WASTE).</summary>
    public class StockMovement
    {
        public int MovementId { get; set; }
        public int HotelId { get; set; }
        public int ItemId { get; set; }
        public string MovementType { get; set; } = "IN"; // IN|OUT|ADJUSTMENT|WASTE
        public decimal Quantity { get; set; }
        public decimal CostPerUnit { get; set; }
        public decimal TotalCost { get; set; }
        public string? ReferenceType { get; set; }   // Order|Manual|Adjustment|Wastage
        public int? ReferenceId { get; set; }        // OrderId when auto-deducted
        public string? Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        // Joined
        public string? ItemName { get; set; }
        public string? Unit { get; set; }
        public string? CreatedByName { get; set; }
    }

    /// <summary>Aggregated inventory dashboard stats.</summary>
    public class InventoryDashboardStats
    {
        public int TotalItems { get; set; }
        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
        public int TotalRecipes { get; set; }
        public decimal TotalStockValue { get; set; }
        public int TodayMovements { get; set; }
        public List<InventoryItem> LowStockList { get; set; } = new();
    }
}
