using Mouzie.Data.DataAccess;
using Mouzie.Data.Models.Domain;
using Mouzie.Data.Repositories.Interfaces;

namespace Mouzie.Data.Repositories.Implementations;

public class SiteSettingRepository : ISiteSettingRepository
{
    private readonly ISqlDataAccess _db;
    public SiteSettingRepository(ISqlDataAccess db) => _db = db;

    public async Task<IEnumerable<SiteSetting>> GetAllSettingsAsync()
        => await _db.QueryAsync<SiteSetting>("usp_Settings_GetAll");

    public async Task<string?> GetSettingByKeyAsync(string key)
        => await _db.ExecuteScalarAsync<string>("usp_Settings_GetByKey", new { SettingKey = key });

    public async Task UpsertSettingAsync(string key, string value)
        => await _db.ExecuteAsync("usp_Settings_Upsert", new { SettingKey = key, SettingValue = value });
}