using System;
using System.Collections.Generic;
using System.Text;

namespace Mouzie.Data.Models.Domain
{
    public class HousekeepingLog
    {
        public int LogId { get; set; }
        public int RoomId { get; set; }
        public DateTime LogDate { get; set; } = DateTime.UtcNow;
        public string? CleanedBy { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "Pending";
        public bool IsDeleted { get; set; } = false;
    }
}
