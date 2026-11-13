using System.ComponentModel.DataAnnotations;

namespace Mouzie.Data.Models.Domain
{
    public class SiteSetting
    {
        public string SettingKey { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
    }

    
}