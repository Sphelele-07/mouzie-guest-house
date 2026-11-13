using System.ComponentModel.DataAnnotations;
using Mouzie.Data.Validation;

namespace Mouzie.Data.Models.ViewModels
{
    public class BookingRequestVM
    {
        [Required]
        public int RoomId { get; set; }

        [Required, StringLength(100, MinimumLength = 2)]
        [RegularExpression(@"^[A-Za-zÀ-ÿ'\- ]+$", ErrorMessage = "Name may only contain letters, spaces, hyphens and apostrophes.")]
        [Display(Name = "Full name")]
        public string GuestName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        [Display(Name = "Email address")]
        public string GuestEmail { get; set; } = string.Empty;

        [Required, SaPhone, StringLength(10, MinimumLength = 10)]
        [Display(Name = "Phone number")]
        public string GuestPhone { get; set; } = string.Empty;

        [Required, SaIdNumber, StringLength(13, MinimumLength = 13)]
        [Display(Name = "South African ID number")]
        public string GuestIdNumber { get; set; } = string.Empty;

        [Display(Name = "Foreign national (no SA ID)")]
        public bool GuestIsForeign { get; set; } = false;

        [Required, DataType(DataType.Date)]
        [Display(Name = "Booking date")]
        public DateTime BookingDate { get; set; } = DateTime.Today.AddDays(1);
    }
}