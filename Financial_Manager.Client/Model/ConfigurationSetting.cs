namespace Financial_Manager.Client.Model
{
    public class ConfigurationSetting
    {
        public string LocalDataBaseConnectionString { get; set; } = null!;
        public string ServerConnectionString { get; set; } = null!;
        public bool IsLogged { get; set; }
        public string CurrentPageName { get; set; } = null!;
    }
}
