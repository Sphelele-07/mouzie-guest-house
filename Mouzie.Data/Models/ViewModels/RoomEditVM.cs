using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Mouzie.Data.Models.ViewModels
{
    public class RoomEditVM
    {
        public int RoomId { get; set; }

        [Required, StringLength(10, MinimumLength = 1)]
        [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "Room number may only contain letters, digits and hyphens.")]
        [Display(Name = "Room number")]
        public string RoomNumber { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Room type")]
        public string RoomType { get; set; } = "Standard";

        [Required, Range(0.01, 999999.99, ErrorMessage = "Price must be a positive amount.")]
        [Display(Name = "Price per night (R)")]
        public decimal PricePerNight { get; set; } = 600m;

        [Required, StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Available";

        [StringLength(50)]
        [Display(Name = "Room key / lockbox code")]
        public string? RoomKeyCode { get; set; }

        [StringLength(200)]
        [Display(Name = "Image path")]
        public string? ImagePath { get; set; }
    }
}
