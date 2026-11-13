using Mouzie.Data.Repositories.Interfaces;

namespace Mouzie.UI.Services;

public interface IBookingCodeGenerator
{
    Task<string> GenerateUniqueAsync();
}

public class BookingCodeGenerator : IBookingCodeGenerator
{
    private readonly IBookingRepository _bookings;
    private readonly Random _rng = new();

    public BookingCodeGenerator(IBookingRepository bookings) => _bookings = bookings;

    public async Task<string> GenerateUniqueAsync()
    {
        // Try up to 20 times — collision probability is effectively zero
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var code = $"MZ-{_rng.Next(1000, 10000)}";
            var existing = await _bookings.GetByCodeAsync(code);
            if (existing is null) return code;
        }
        // Fallback: timestamp-based (still rare)
        return $"MZ-{DateTime.UtcNow:HHmmss}";
    }
}