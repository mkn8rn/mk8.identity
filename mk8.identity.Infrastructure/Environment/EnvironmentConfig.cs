namespace mk8.identity.Infrastructure.Environment
{
    public class EnvironmentConfig
    {
        public required string ConnectionString { get; set; }
        public required AdminCredentialsConfig AdminCredentials { get; set; }
    }

    public class AdminCredentialsConfig
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
