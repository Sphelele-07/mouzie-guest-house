using Mouzie.Data.Models.Domain;

namespace Mouzie.Data.Repositories.Interfaces
{
    public interface IRoomRepository
    {
        Task<IEnumerable<Room>> GetAllAsync(bool includeInactive = false);
        Task<Room?> GetByIdAsync(int id);
        Task<int> InsertAsync(Room room);
        Task UpdateAsync(Room room);
        Task SoftDeleteAsync(int id);          // sets IsActive = 0
        Task RestoreAsync(int id);             // sets IsActive = 1
    }
}