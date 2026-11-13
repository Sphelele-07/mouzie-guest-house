using Mouzie.Data.Models.Domain;

namespace Mouzie.Data.Repositories.Interfaces
{
    public interface IBookingRepository
    {
        // Read
        Task<Booking?> GetByIdAsync(int id);
        Task<Booking?> GetByCodeAsync(string code);
        Task<IEnumerable<Booking>> GetAllAsync(string? status = null);
        Task<IEnumerable<Booking>> GetAllPendingAsync();
        Task<IEnumerable<Booking>> GetByRoomAndDateAsync(int roomId, DateTime date);
        Task<IEnumerable<Booking>> GetByGuestIdNumberAsync(string idNumber);   // find duplicate / history

        // Write
        Task<int> CreateBookingAsync(Booking booking);
        Task UpdateStatusAsync(int bookingId, string status, DateTime? timestamp = null);
        Task SetCheckInAsync(int bookingId, DateTime time);
        Task SetCheckOutAsync(int bookingId, DateTime time, bool isEarly, string? reason);

        // Soft delete (admin only)
        Task SoftDeleteAsync(int bookingId);
        Task RestoreAsync(int bookingId);
    }
}