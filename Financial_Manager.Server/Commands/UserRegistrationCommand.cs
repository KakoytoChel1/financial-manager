namespace Financial_Manager.Server.Commands
{
    public class UserRegistrationCommand
    {
        private readonly ILogger<UserRegistrationCommand> _logger;

        public UserRegistrationCommand(ILogger<UserRegistrationCommand> logger)
        {
            _logger = logger;
        }

        public void Register(string name, string surname)
        {
            _logger.LogInformation($"Registered new user: {name} {surname}");
        }
    }
}
