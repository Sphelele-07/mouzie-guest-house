using Mouzie.Data.DataAccess;
using Mouzie.Data.Models.Domain;
using Mouzie.Data.Repositories.Interfaces;

namespace Mouzie.Data.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private readonly ISqlDataAccess _db;
    public UserRepository(ISqlDataAccess db) => _db = db;

    public async Task<User?> GetByUsernameAsync(string username)
        => await _db.QuerySingleAsync<User>("usp_User_GetByUsername", new { Username = username });

    public async Task<User?> GetByIdAsync(int userId)
        => await _db.QuerySingleAsync<User>("usp_User_GetById", new { UserId = userId });

    public async Task<int> InsertAsync(User u)
        => await _db.ExecuteScalarAsync<int>("usp_User_Insert",
            new { u.Username, u.PasswordHash });

    public async Task UpdatePasswordAsync(int userId, string newHash)
        => await _db.ExecuteAsync("usp_User_UpdatePassword",
            new { UserId = userId, PasswordHash = newHash });

    public async Task UpdateLastLoginAsync(int userId, DateTime when)
        => await _db.ExecuteAsync("usp_User_UpdateLastLogin",
            new { UserId = userId, LastLoginAt = when });

    public async Task UpdateProfileAsync(int userId, string firstName, string lastName, string phone, string profileImagePath)
        => await _db.ExecuteAsync("usp_User_UpdateProfile",
            new { UserId = userId, FirstName = firstName, LastName = lastName, Phone = phone, ProfileImagePath = profileImagePath });

    public async Task DeactivateAsync(int userId)
        => await _db.ExecuteAsync("usp_User_Deactivate", new { UserId = userId });
}