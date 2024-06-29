using Financial_Manager.Client.Model;

namespace Financial_Manager.Client.Services
{
    public interface IConfigurationService
    {
        ConfigurationSetting? GetConfigurationSettings();

        void SaveConfigurationSettings(ConfigurationSetting config);
    }
}
