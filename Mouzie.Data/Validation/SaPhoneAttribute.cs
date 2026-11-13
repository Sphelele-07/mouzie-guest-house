using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Mouzie.Data.Validation;

/// <summary>
/// South African phone: exactly 10 digits, starting with 0.
/// Accepts 0XXXXXXXXX (mobile 06/07/08, landline 01-05).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class SaPhoneAttribute : ValidationAttribute
{
    private static readonly Regex Pattern = new(@"^0\d{9}$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;

        s = s.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (s.StartsWith("+27")) s = "0" + s[3..];
        if (s.StartsWith("27") && s.Length == 11) s = "0" + s[2..];

        if (!Pattern.IsMatch(s))
            return new ValidationResult("Phone number must be exactly 10 digits, starting with 0 (e.g. 0821234567).");

        return ValidationResult.Success;
    }
}