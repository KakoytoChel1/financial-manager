using Financial_Manager.Client.Model;
using System.Text.Json;
using Windows.Storage;

namespace Financial_Manager.Client.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private const string ConfigKey = "AppConfig";

        public ConfigurationSetting? GetConfigurationSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(ConfigKey, out object? jsonObj) && jsonObj is string json)
            {
                return JsonSerializer.Deserialize<ConfigurationSetting>(json);
            }
            return null;
        }

        public void SaveConfigurationSettings(ConfigurationSetting config)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            string json = JsonSerializer.Serialize(config);
            localSettings.Values[ConfigKey] = json;
        }
    }
}
