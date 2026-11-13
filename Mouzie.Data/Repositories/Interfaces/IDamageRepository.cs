using Mouzie.Data.Models.Domain;

namespace Mouzie.Data.Repositories.Interfaces;

public interface IDamageRepository
{
    Task<int> InsertAsync(DamageRecord record);
    Task<DamageRecord?> GetByIdAsync(int damageId);
    Task<IEnumerable<DamageRecord>> GetByBookingIdAsync(int bookingId);
    Task<IEnumerable<DamageRecord>> GetAllAsync();
}