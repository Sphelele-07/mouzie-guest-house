using Mouzie.Data.DataAccess;
using Mouzie.Data.Models.Domain;
using Mouzie.Data.Repositories.Interfaces;

namespace Mouzie.Data.Repositories.Implementations;

public class RoomRepository : IRoomRepository
{
    private readonly ISqlDataAccess _db;
    public RoomRepository(ISqlDataAccess db) => _db = db;

    public async Task<IEnumerable<Room>> GetAllAsync(bool includeInactive = false)
        => await _db.QueryAsync<Room>("usp_Room_GetAll", new { IncludeInactive = includeInactive });

    public async Task<Room?> GetByIdAsync(int id)
        => await _db.QuerySingleAsync<Room>("usp_Room_GetById", new { RoomId = id });

    public async Task<int> InsertAsync(Room room)
        => await _db.ExecuteScalarAsync<int>("usp_Room_Insert", new
        {
            room.RoomNumber,
            room.RoomType,
            room.PricePerNight,
            room.Status,
            room.RoomKeyCode,
            room.ImagePath
        });

    public async Task UpdateAsync(Room room)
        => await _db.ExecuteAsync("usp_Room_Update", new
        {
            room.RoomId,
            room.RoomNumber,
            room.RoomType,
            room.PricePerNight,
            room.Status,
            room.RoomKeyCode,
            room.IsActive,
            room.ImagePath
        });

    public async Task SoftDeleteAsync(int id)
        => await _db.ExecuteAsync("usp_Room_SoftDelete", new { RoomId = id });

    public async Task RestoreAsync(int id)
        => await _db.ExecuteAsync("usp_Room_Restore", new { RoomId = id });
}