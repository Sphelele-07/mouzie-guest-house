using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Mouzie.Data.Validation;

/// <summary>
/// Validates a South African ID number:
///   - 13 digits only
///   - first 6 = YYMMDD (date of birth)
///   - position 11 = citizenship (0 = SA citizen, 1 = permanent resident)
///   - position 13 = Luhn checksum digit
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class SaIdNumberAttribute : ValidationAttribute
{
    public bool ValidateChecksum { get; set; } = true;

    private static readonly Regex Digits13 = new(@"^\d{13}$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;

        s = s.Trim();

        if (!Digits13.IsMatch(s))
            return new ValidationResult("SA ID number must be exactly 13 digits (no spaces, no letters).");

        // Date of birth (YYMMDD)
        if (!int.TryParse(s[..2], out var yy) ||
            !int.TryParse(s.Substring(2, 2), out var mm) ||
            !int.TryParse(s.Substring(4, 2), out var dd))
            return new ValidationResult("SA ID number contains an invalid date of birth.");

        var year = yy < 30 ? 2000 + yy : 1900 + yy;
        try { _ = new DateTime(year, mm, dd); }
        catch { return new ValidationResult("SA ID number contains an invalid date of birth."); }

        // Citizenship digit must be 0 or 1
        if (s[10] is not ('0' or '1'))
            return new ValidationResult("SA ID number has an invalid citizenship digit.");

        if (ValidateChecksum && !PassesLuhn(s))
            return new ValidationResult("SA ID number failed the checksum test — please re-check the digits.");

        return ValidationResult.Success;
    }

    private static bool PassesLuhn(string id)
    {
        int sum = 0;
        bool alt = false;
        for (int i = id.Length - 1; i >= 0; i--)
        {
            int n = id[i] - '0';
            if (alt) { n *= 2; if (n > 9) n -= 9; }
            sum += n;
            alt = !alt;
        }
        return sum % 10 == 0;
    }
}