using Mouzie.Data.DataAccess;
using Mouzie.Data.Models.Domain;
using Mouzie.Data.Repositories.Interfaces;

namespace Mouzie.Data.Repositories.Implementations;

public class DamageRepository : IDamageRepository
{
    private readonly ISqlDataAccess _db;
    public DamageRepository(ISqlDataAccess db) => _db = db;

    public async Task<int> InsertAsync(DamageRecord r)
        => await _db.ExecuteScalarAsync<int>("usp_Damage_Insert", new
        {
            r.BookingId,
            r.Description,
            r.PenaltyAmount,
            r.Severity
        });

    // 👇 NEW
    public async Task<DamageRecord?> GetByIdAsync(int damageId)
        => await _db.QuerySingleAsync<DamageRecord>("usp_Damage_GetById", new { DamageId = damageId });

    public async Task<IEnumerable<DamageRecord>> GetByBookingIdAsync(int bookingId)
        => await _db.QueryAsync<DamageRecord>("usp_Damage_GetByBookingId", new { BookingId = bookingId });

    public async Task<IEnumerable<DamageRecord>> GetAllAsync()
        => await _db.QueryAsync<DamageRecord>("usp_Damage_GetAll");
}