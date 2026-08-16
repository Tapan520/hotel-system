using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotelChannelManager.DTOs
{
    // ── API Response Wrapper ─────────────────────────────────────────
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }
        public int? TotalCount { get; set; }
        public string? ErrorCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> Ok(T data, string message = "Success", int? total = null)
            => new() { Success = true, Message = message, Data = data, TotalCount = total };

        public static ApiResponse<T> Fail(string message, string? code = null)
            => new() { Success = false, Message = message, ErrorCode = code };
    }

    // ── Auth DTOs ────────────────────────────────────────────────────
    public class LoginRequest
    {
        [Required] public string Username { get; set; } = "";
        [Required] public string Password { get; set; } = "";
    }

    public class LoginResponse
    {
        public string Token { get; set; } = "";
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public int? HotelId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required] public string CurrentPassword { get; set; } = "";
        [Required, MinLength(8)] public string NewPassword { get; set; } = "";
    }

    // ── Booking Add-On DTOs ──────────────────────────────────────────────
    public class BookingAddonRequest
    {
        [Required] public int AddonId { get; set; }
        public int Quantity { get; set; } = 1; // persons for PerPerson types; nights auto-computed
    }

    public class AddonQuoteLine
    {
        public int AddonId { get; set; }
        public string AddonName { get; set; } = "";
        public string ChargeType { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal Quantity { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
    }

    // ── Booking DTOs ─────────────────────────────────────────────────
    public class PriceQuoteRequest
    {
        [Required] public int RoomTypeId { get; set; }
        [Required] public DateTime CheckInDate { get; set; }
        [Required] public DateTime CheckOutDate { get; set; }
        public int? PartnerId { get; set; }
        public int Adults { get; set; } = 2;
        public List<BookingAddonRequest> Addons { get; set; } = new();
    }

    public class NightlyRate
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; } = "";
        public decimal Rate { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsWeekend { get; set; }
    }

    public class PriceQuoteResponse
    {
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = "";
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int Nights { get; set; }
        public List<NightlyRate> NightlyRates { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal AddonTotal { get; set; }
        public List<AddonQuoteLine> AddonLines { get; set; } = new();
        public decimal GrandTotal { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal CommissionPercent { get; set; }
        public decimal NetToHotel { get; set; }
        public bool IsAvailable { get; set; }
        public string? UnavailableReason { get; set; }
        public string CurrencyCode { get; set; } = "INR";
    }

    public class CreateBookingRequest
    {
        [Required] public int HotelId { get; set; } = 1;
        [Required] public int RoomTypeId { get; set; }
        [Required] public DateTime CheckInDate { get; set; }
        [Required] public DateTime CheckOutDate { get; set; }
        public int AdultsCount { get; set; } = 1;
        public int ChildrenCount { get; set; } = 0;
        [Required] public string PaymentMode { get; set; } = "PayAtHotel";
        public string BookingSource { get; set; } = "HotelDesk";
        public int? PartnerId { get; set; }
        public string? SpecialRequests { get; set; }
        // Admin-only: override the standard nightly rate for this booking
        public decimal? OverrideRoomRate { get; set; }
        // Selected add-ons (Extra Bed, Breakfast, Dinner, etc.)
        public List<BookingAddonRequest> Addons { get; set; } = new();
        // Guest info
        [Required] public string GuestFirstName { get; set; } = "";
        [Required] public string GuestLastName { get; set; } = "";
        [Required, EmailAddress] public string GuestEmail { get; set; } = "";
        public string? GuestPhone { get; set; }
        public string? GuestNationality { get; set; }
        public string? GuestIDType { get; set; }
        public string? GuestIDNumber { get; set; }
        public string? GuestAddress { get; set; }
        public string? GuestCity { get; set; }
        public string? GuestCountry { get; set; }
    }

    public class CancelBookingRequest
    {
        [Required] public int BookingId { get; set; }
        public string? Reason { get; set; }
    }

    public class AssignRoomRequest
    {
        [Required] public int BookingId { get; set; }
        [Required] public int RoomId { get; set; }
    }

    // ── Rate DTOs ────────────────────────────────────────────────────
    public class BulkRateRequest
    {
        [Required] public int RoomTypeId { get; set; }
        [Required] public DateTime FromDate { get; set; }
        [Required] public DateTime ToDate { get; set; }
        [Required, Range(0, 9999999)] public decimal BaseRate { get; set; }
        public decimal? SpecialRate { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int MinNights { get; set; } = 1;
        public string? Notes { get; set; }
    }

    public class BlockAvailabilityRequest
    {
        [Required] public int RoomTypeId { get; set; }
        [Required] public DateTime FromDate { get; set; }
        [Required] public DateTime ToDate { get; set; }
        [Required, Range(1, 100)] public int BlockCount { get; set; } = 1;
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Request body for POST /api/availability/init
    /// Bulk-seeds roomavailability rows so all rooms show correct status
    /// on a fresh Railway deployment before any bookings have been made.
    /// </summary>
    public class InitAvailabilityRequest
    {
        [Required] public DateTime FromDate { get; set; } = DateTime.Today;
        [Required] public DateTime ToDate   { get; set; } = DateTime.Today.AddMonths(12);
        public int? RoomTypeId { get; set; } // null = all room types
    }

    // ── Payment DTOs ──────────────────────────────────────────────────
    public class RecordPaymentRequest
    {
        [Required] public int BookingId { get; set; }
        [Required, Range(0.01, 9999999)] public decimal Amount { get; set; }
        public string PaymentType { get; set; } = "Full";
        [Required] public string PaymentMethod { get; set; } = "Cash";
        public string? TransactionRef { get; set; }
        public string? GatewayName { get; set; }
        public string? GatewayTxnId { get; set; }
        public string? Notes { get; set; }
    }

    // ── Report DTOs ──────────────────────────────────────────────────
    public class ReportFilter
    {
        public DateTime FromDate { get; set; } = DateTime.Today.AddMonths(-1);
        public DateTime ToDate { get; set; } = DateTime.Today;
        public int? HotelId { get; set; }
        public int? PartnerId { get; set; }
        public string? Status { get; set; }
        public string? BookingSource { get; set; }
        public string? RoomTypeId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    // ── User DTOs ────────────────────────────────────────────────────
    public class CreateUserRequest
    {
        [Required] public string Username { get; set; } = "";
        [Required, MinLength(8)] public string Password { get; set; } = "";
        public string? FullName { get; set; }
        [EmailAddress] public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Role { get; set; } = "FrontDesk";
        public int? HotelId { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? FullName { get; set; }
        [EmailAddress] public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public int? HotelId { get; set; }
    }

    // ── OMS DTOs ─────────────────────────────────────────────────────────────

    public class CreateOrderRequest
    {
        // "InRoom" = hotel room guest  |  "DirectSale" = restaurant/bar walk-in
        [Required] public string OrderType { get; set; } = "InRoom";

        // ── InRoom fields (required when OrderType = InRoom) ──────────────────
        public int? BookingId { get; set; }
        public int? RoomId { get; set; }
        public int? CustomerId { get; set; }

        // ── DirectSale fields (used when OrderType = DirectSale) ──────────────
        public string? WalkInGuestName { get; set; }
        public string? WalkInGuestPhone { get; set; }

        // ── Common fields ─────────────────────────────────────────────────────
        [Required] public string Category { get; set; } = "";
        public string? Priority { get; set; }
        public string? SpecialInstructions { get; set; }
        public DateTime? DeliveryTime { get; set; }
        [Required, MinLength(1)] public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public int? CatalogId { get; set; }
        [Required] public string ItemName { get; set; } = "";
        public string? Description { get; set; }
        [Range(0.01, 9999)] public decimal Quantity { get; set; } = 1;
        [Required, Range(0, 9999999)] public decimal UnitPrice { get; set; }
        [Range(0, 100)] public decimal TaxPercent { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        [Required] public string Status { get; set; } = "";
        public string? Notes { get; set; }
    }

    public class ManualBillEntryRequest
    {
        [Required] public int BookingId { get; set; }
        [Required] public string EntryType { get; set; } = "ManualCharge";
        [Required] public string Description { get; set; } = "";
        [Required, Range(0.01, 9999999)] public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
    }

    public class VoidEntryRequest
    {
        public string? Reason { get; set; }
    }

    // ── RECIPE-BASED IMS DTOs ────────────────────────────────────────────────

    public class CreateInventoryItemRequest
    {
        [Required] public string ItemName { get; set; } = "";
        public string? Description { get; set; }
        [Required] public string Category { get; set; } = "Ingredient";
        [Required] public string Unit { get; set; } = "kg";
        [Range(0, 999999)] public decimal CurrentStock { get; set; }
        [Range(0, 999999)] public decimal MinStockLevel { get; set; }
        [Range(0, 999999)] public decimal ReorderQty { get; set; }
        [Range(0, 9999999)] public decimal CostPerUnit { get; set; }
        public string? Supplier { get; set; }
    }

    public class CreateRecipeRequest
    {
        [Required] public string RecipeName { get; set; } = "";
        public string? Description { get; set; }
        [Required] public string Category { get; set; } = "";
        [Range(1, 9999)] public int Yield { get; set; } = 1;
        public string? Instructions { get; set; }
        // Selling price — used to auto-create/update the linked ordercatalog entry
        [Required, Range(0, 9999999)] public decimal SellingPrice { get; set; }
        public string Unit { get; set; } = "per portion";
        [Range(0, 100)] public decimal TaxPercent { get; set; }
        [Required, MinLength(1)] public List<RecipeIngredientRequest> Ingredients { get; set; } = new();
    }

    public class RecipeIngredientRequest
    {
        [Required] public int ItemId { get; set; }
        [Required, Range(0.001, 99999)] public decimal Quantity { get; set; }
        public string? Notes { get; set; }
    }

    public class StockMovementRequest
    {
        [Required] public int ItemId { get; set; }
        [Required] public string MovementType { get; set; } = "IN"; // IN|OUT|ADJUSTMENT|WASTE
        [Required, Range(0.001, 999999)] public decimal Quantity { get; set; }
        [Range(0, 9999999)] public decimal CostPerUnit { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? Notes { get; set; }
    }

    public class DeductRecipeStockRequest
    {
        [Required] public int RecipeId { get; set; }
        [Range(1, 9999)] public int Portions { get; set; } = 1;
        public int? OrderId { get; set; }
        public string? Notes { get; set; }
    }

    // ── CUSTOMER PORTAL AUTH ────────────────────────────────────────────────
    public class CustomerLoginRequest
    {
        [Required] public string Email { get; set; } = "";
        [Required] public string BookingReference { get; set; } = "";
    }

    public class CustomerLoginResponse
    {
        public string Token       { get; set; } = "";
        public string GuestName   { get; set; } = "";
        public string Email       { get; set; } = "";
        public int    TotalStays  { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
