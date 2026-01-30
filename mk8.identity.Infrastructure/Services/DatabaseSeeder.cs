using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using mk8.identity.Infrastructure.Contexts;
using mk8.identity.Infrastructure.Environment;
using mk8.identity.Infrastructure.Helpers;
using mk8.identity.Infrastructure.Models.Identity;

namespace mk8.identity.Infrastructure.Services
{
    public class DatabaseSeeder
    {
        private readonly IdentityContext _identityContext;
        private readonly ApplicationContext _applicationContext;
        private readonly EnvironmentConfig _config;

        public DatabaseSeeder(
            IdentityContext identityContext,
            ApplicationContext applicationContext,
            EnvironmentConfig config)
        {
            _identityContext = identityContext;
            _applicationContext = applicationContext;
            _config = config;
        }

        public async Task SeedAsync()
        {
            // Seed admin if none exists
            await SeedAdminUserAsync();
        }

        private async Task SeedAdminUserAsync()
        {
            var adminRole = await _identityContext.Roles
                .FirstOrDefaultAsync(r => r.RoleName == RoleType.Administrator);

            if (adminRole == null)
                return;

            var hasAdmin = await _identityContext.UserRoles
                .AnyAsync(ur => ur.RoleId == adminRole.Id);

            if (hasAdmin)
                return;

            // Check if admin username already exists
            var existingUser = await _identityContext.Users
                .FirstOrDefaultAsync(u => u.Username == _config.AdminCredentials.Username);

            if (existingUser != null)
            {
                // Just assign role to existing user
                _identityContext.UserRoles.Add(new UserRoleDB
                {
                    UserId = existingUser.Id,
                    RoleId = adminRole.Id,
                    AssignedAt = DateTimeOffset.UtcNow
                });
                await _identityContext.SaveChangesAsync();
                return;
            }

            // Create new admin user
            var salt = PasswordHelper.GenerateSalt();
            var hash = PasswordHelper.HashPassword(_config.AdminCredentials.Password, salt);

            var adminUser = new UserDB
            {
                Id = Guid.NewGuid(),
                Username = _config.AdminCredentials.Username,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _identityContext.Users.Add(adminUser);
            await _identityContext.SaveChangesAsync();

            // Assign admin role
            _identityContext.UserRoles.Add(new UserRoleDB
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                AssignedAt = DateTimeOffset.UtcNow
            });
            await _identityContext.SaveChangesAsync();

            // Create membership for admin
            var membership = new Models.Application.UserMembershipDB
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id,
                IsActive = true,
                StartDate = DateTimeOffset.UtcNow,
                ActivationDates = [DateTimeOffset.UtcNow],
                DeactivationDates = [],
                Privileges = new Models.Application.PrivilegesDB
                {
                    Id = Guid.NewGuid(),
                    VotingRights = true
                }
            };
            _applicationContext.Memberships.Add(membership);
            await _applicationContext.SaveChangesAsync();

            Console.WriteLine($"Admin user '{_config.AdminCredentials.Username}' created successfully.");
        }
    }

    public static class DatabaseSeederExtensions
    {
        public static async Task SeedDatabaseAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
    }
}
