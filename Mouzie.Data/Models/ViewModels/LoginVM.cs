using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Mouzie.Data.Models.ViewModels
{
    public class LoginVM
    {
        [Required, EmailAddress, StringLength(150)]
        [Display(Name = "Email")]
        public string Username { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
