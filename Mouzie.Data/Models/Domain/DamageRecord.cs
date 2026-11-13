

namespace Mouzie.Data.Models.Domain
{
    public class DamageRecord
    {
        public int DamageId { get; set; }
        public int BookingId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal PenaltyAmount { get; set; }
        public DateTime ReportedDate { get; set; } = DateTime.UtcNow;
        public string Severity { get; set; } = "Minor";
        public bool IsDeleted { get; set; } = false;

        // 👇 These two properties are required by the Razor view
        public string? GuestName { get; set; }
        public string? BookingCode { get; set; }
    }
}
