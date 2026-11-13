using Mouzie.Data.DataAccess;
using Mouzie.Data.Models.Domain;
using Mouzie.Data.Repositories.Interfaces;

namespace Mouzie.Data.Repositories.Implementations;

public class BookingRepository : IBookingRepository
{
    private readonly ISqlDataAccess _db;
    public BookingRepository(ISqlDataAccess db) => _db = db;

    public async Task<Booking?> GetByIdAsync(int id)
        => await _db.QuerySingleAsync<Booking>("usp_Booking_GetById", new { BookingId = id });

    public async Task<Booking?> GetByCodeAsync(string code)
        => await _db.QuerySingleAsync<Booking>("usp_Booking_GetByCode", new { BookingCode = code });

    public async Task<IEnumerable<Booking>> GetAllAsync(string? status = null)
        => await _db.QueryAsync<Booking>("usp_Booking_GetAll", new { Status = status });

    public async Task<IEnumerable<Booking>> GetAllPendingAsync()
        => await _db.QueryAsync<Booking>("usp_Booking_GetAllPending");

    public async Task<IEnumerable<Booking>> GetByRoomAndDateAsync(int roomId, DateTime date)
        => await _db.QueryAsync<Booking>("usp_Booking_GetByRoomAndDate",
            new { RoomId = roomId, BookingDate = date.Date });

    public async Task<IEnumerable<Booking>> GetByGuestIdNumberAsync(string idNumber)
        => await _db.QueryAsync<Booking>("usp_Booking_GetByGuestIdNumber",
            new { GuestIdNumber = idNumber.Trim() });

    public async Task<int> CreateBookingAsync(Booking b)
        => await _db.ExecuteScalarAsync<int>("usp_Booking_Insert", new
        {
            b.RoomId,
            b.GuestName,
            b.GuestEmail,
            b.GuestPhone,
            b.GuestIdNumber,
            b.GuestIsForeign,
            b.BookingDate,
            b.Status,
            b.BookingCode
        });

    public async Task UpdateStatusAsync(int bookingId, string status, DateTime? timestamp = null)
        => await _db.ExecuteAsync("usp_Booking_UpdateStatus",
            new { BookingId = bookingId, Status = status, Timestamp = timestamp });

    public async Task SetCheckInAsync(int bookingId, DateTime time)
        => await _db.ExecuteAsync("usp_Booking_SetCheckIn",
            new { BookingId = bookingId, CheckInTime = time });

    public async Task SetCheckOutAsync(int bookingId, DateTime time, bool isEarly, string? reason)
        => await _db.ExecuteAsync("usp_Booking_SetCheckOut",
            new { BookingId = bookingId, CheckOutTime = time, IsEarlyCheckout = isEarly, EarlyCheckoutReason = reason });

    public async Task SoftDeleteAsync(int bookingId)
        => await _db.ExecuteAsync("usp_Booking_SoftDelete", new { BookingId = bookingId });

    public async Task RestoreAsync(int bookingId)
        => await _db.ExecuteAsync("usp_Booking_Restore", new { BookingId = bookingId });
}