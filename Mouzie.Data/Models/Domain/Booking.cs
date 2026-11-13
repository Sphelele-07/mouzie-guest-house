using System.ComponentModel.DataAnnotations;

namespace Mouzie.Data.Models.Domain
{
    public class Booking
    {
        public int BookingId { get; set; }
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        // ── Guest details ─────────────────────────────────────────────
        public string GuestName { get; set; } = string.Empty;
        public string GuestEmail { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;   // 10 digits
        public string GuestIdNumber { get; set; } = string.Empty;   // 13 digits — SENSITIVE (POPIA)

        /// <summary>True if the guest is a foreign national without a SA ID.
        /// When true, GuestIdNumber may store a passport number instead.</summary>
        public bool GuestIsForeign { get; set; }

        // ── Stay ──────────────────────────────────────────────────────
        public DateTime BookingDate { get; set; }                   // the night of stay
        public string Status { get; set; } = "Pending";      // Pending | Approved | Rejected | CheckedIn | CheckedOut | Cancelled
        public string BookingCode { get; set; } = string.Empty;   // MZ-4921

        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public bool IsEarlyCheckout { get; set; }
        public string? EarlyCheckoutReason { get; set; }

        // ── Audit ─────────────────────────────────────────────────────
        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;         // soft delete (admin only)
    }
}