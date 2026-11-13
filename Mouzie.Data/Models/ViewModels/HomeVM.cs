using Mouzie.Data.Models.Domain;

namespace Mouzie.Data.Models.ViewModels
{
    public class HomeVM
    {
        public List<Room> Rooms { get; set; } = new();
        public string WhatsAppNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string BusinessName { get; set; } = "Mouzie Guest House";
        public string Tagline { get; set; } = "Your comfort is our priority.";
        public string CheckInTime { get; set; } = "14:00";
        public string CheckOutTime { get; set; } = "10:00";
    }
}