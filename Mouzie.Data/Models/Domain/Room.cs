using System;
using System.Collections.Generic;
using System.Text;

namespace Mouzie.Data.Models.Domain
{
    public class Room
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;   // "1", "2"
        public string RoomType { get; set; } = "Standard";       // Standard | Ensuite
        public decimal PricePerNight { get; set; } = 600m;
        public string Status { get; set; } = "Available";        // Available | Occupied | Maintenance
        public string? RoomKeyCode { get; set; }
        public bool IsActive { get; set; } = true;               // soft delete
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? ImagePath { get; set; }                   // /images/rooms/room1.jpg
    }
}
