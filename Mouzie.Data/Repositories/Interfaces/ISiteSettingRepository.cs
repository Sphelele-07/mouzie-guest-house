using Mouzie.Data.Models.Domain;

namespace Mouzie.Data.Repositories.Interfaces;

public interface ISiteSettingRepository
{
    Task<IEnumerable<SiteSetting>> GetAllSettingsAsync();
    Task<string?> GetSettingByKeyAsync(string key);
    Task UpsertSettingAsync(string key, string value);
}