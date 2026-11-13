using Mouzie.Data.Models.Domain;

namespace Mouzie.Data.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int userId);
    Task<int> InsertAsync(User user);
    Task UpdatePasswordAsync(int userId, string newHash);
    Task UpdateLastLoginAsync(int userId, DateTime when);
    Task UpdateProfileAsync(int userId, string firstName, string lastName, string phone, string profileImagePath);
    Task DeactivateAsync(int userId);
}