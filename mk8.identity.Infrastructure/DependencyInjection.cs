using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using mk8.identity.Infrastructure.Contexts;
using mk8.identity.Infrastructure.Environment;
using mk8.identity.Infrastructure.Services;

namespace mk8.identity.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers Infrastructure layer services (DbContexts) with the dependency injection container.
        /// Loads configuration from environment files.
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, bool isDevelopment = false)
        {
            var config = EnvironmentLoader.Load(isDevelopment);
            return services.AddInfrastructureServices(config);
        }

        /// <summary>
        /// Registers Infrastructure layer services (DbContexts) with the dependency injection container.
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            EnvironmentConfig config)
        {
            services.AddSingleton(config);

            services.AddDbContext<IdentityContext>(options =>
                options.UseNpgsql(config.ConnectionString));

            services.AddDbContext<ApplicationContext>(options =>
                options.UseNpgsql(config.ConnectionString));

            services.AddScoped<DatabaseSeeder>();

            return services;
        }

        /// <summary>
        /// Registers Infrastructure layer services with a custom connection string.
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            string connectionString,
            string adminUsername = "admin",
            string adminPassword = "Admin123!")
        {
            var config = new EnvironmentConfig
            {
                ConnectionString = connectionString,
                AdminCredentials = new AdminCredentialsConfig
                {
                    Username = adminUsername,
                    Password = adminPassword
                }
            };

            return services.AddInfrastructureServices(config);
        }
    }
}
