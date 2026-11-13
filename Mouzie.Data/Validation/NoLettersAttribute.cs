using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Mouzie.Data.Validation;

/// <summary>
/// Use on string fields that must be numeric (e.g. booking code "4921").
/// Rejects any letter, symbol, or space.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class NoLettersAttribute : ValidationAttribute
{
    private static readonly Regex DigitsOnly = new(@"^\d+$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;

        if (!DigitsOnly.IsMatch(s.Trim()))
            return new ValidationResult($"{ctx.DisplayName} may only contain digits.");

        return ValidationResult.Success;
    }
}