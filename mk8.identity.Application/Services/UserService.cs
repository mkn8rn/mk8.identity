using Microsoft.EntityFrameworkCore;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using mk8.identity.Infrastructure.Contexts;
using mk8.identity.Infrastructure.Helpers;
using mk8.identity.Infrastructure.Models.Application;
using mk8.identity.Infrastructure.Models.Identity;
using System.Security.Cryptography;
using DbRoleType = mk8.identity.Infrastructure.Models.Identity.RoleType;

namespace mk8.identity.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IdentityContext _identityContext;
        private readonly ApplicationContext _applicationContext;
        private readonly INotificationService _notificationService;

        public UserService(
            IdentityContext identityContext,
            ApplicationContext applicationContext,
            INotificationService notificationService)
        {
            _identityContext = identityContext;
            _applicationContext = applicationContext;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<UserDTO>> RegisterAsync(UserRegistrationDTO registration)
        {
            if (string.IsNullOrWhiteSpace(registration.Username))
                return ServiceResult<UserDTO>.Fail("Username is required");

            if (string.IsNullOrWhiteSpace(registration.Password) || registration.Password.Length < 8)
                return ServiceResult<UserDTO>.Fail("Password must be at least 8 characters");

            // Check if username already exists
            if (await _identityContext.Users.AnyAsync(u => u.Username == registration.Username))
                return ServiceResult<UserDTO>.Fail("Username already exists");

            // Generate password hash and salt
            var salt = PasswordHelper.GenerateSalt();
            var hash = PasswordHelper.HashPassword(registration.Password, salt);

            var user = new UserDB
            {
                Id = Guid.NewGuid(),
                Username = registration.Username,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _identityContext.Users.Add(user);
            await _identityContext.SaveChangesAsync();

            // Create initial membership status (inactive) with privileges
            var membership = new UserMembershipDB
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                IsActive = false,
                StartDate = DateTimeOffset.UtcNow,
                ActivationDates = [],
                DeactivationDates = [],
                Privileges = new PrivilegesDB
                {
                    Id = Guid.NewGuid(),
                    VotingRights = false
                }
            };
            _applicationContext.Memberships.Add(membership);

            await _applicationContext.SaveChangesAsync();

            // Create notification for new user registration
            await _notificationService.CreateNewUserNotificationAsync(user.Id, user.Username);

            return ServiceResult<UserDTO>.Ok(MapToUserDTO(user));
        }

        public async Task<ServiceResult<AuthTokensDTO>> LoginAsync(UserLoginDTO login)
        {
            var user = await _identityContext.Users
                .FirstOrDefaultAsync(u => u.Username == login.Username);

            if (user == null)
                return ServiceResult<AuthTokensDTO>.Fail("Invalid credentials");

            if (!PasswordHelper.VerifyPassword(login.Password, user.PasswordHash, user.PasswordSalt))
                return ServiceResult<AuthTokensDTO>.Fail("Invalid credentials");

            // Generate tokens
            var refreshToken = new RefreshTokenDB
            {
                Id = Guid.NewGuid(),
                Token = GenerateSecureToken(),
                UserId = user.Id,
                IssuedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
            };

            var accessToken = new AccessTokenDB
            {
                Id = Guid.NewGuid(),
                Token = GenerateSecureToken(),
                UserId = user.Id,
                RefreshTokenId = refreshToken.Id,
                IssuedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            };

            _identityContext.RefreshTokens.Add(refreshToken);
            _identityContext.AccessTokens.Add(accessToken);
            await _identityContext.SaveChangesAsync();

            return ServiceResult<AuthTokensDTO>.Ok(new AuthTokensDTO
            {
                AccessToken = new AccessTokenDTO { Token = accessToken.Token, ExpiresAt = accessToken.ExpiresAt },
                RefreshToken = new RefreshTokenDTO { Token = refreshToken.Token, ExpiresAt = refreshToken.ExpiresAt }
            });
        }

        public async Task<ServiceResult<AuthTokensDTO>> RefreshTokenAsync(string refreshToken)
        {
            var token = await _identityContext.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (token == null || token.IsRevoked || token.ExpiresAt < DateTimeOffset.UtcNow)
                return ServiceResult<AuthTokensDTO>.Fail("Invalid or expired refresh token");

            // Generate new access token
            var accessToken = new AccessTokenDB
            {
                Id = Guid.NewGuid(),
                Token = GenerateSecureToken(),
                UserId = token.UserId,
                RefreshTokenId = token.Id,
                IssuedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            };

            _identityContext.AccessTokens.Add(accessToken);
            await _identityContext.SaveChangesAsync();

            return ServiceResult<AuthTokensDTO>.Ok(new AuthTokensDTO
            {
                AccessToken = new AccessTokenDTO { Token = accessToken.Token, ExpiresAt = accessToken.ExpiresAt },
                RefreshToken = new RefreshTokenDTO { Token = token.Token, ExpiresAt = token.ExpiresAt }
            });
        }

        public async Task<ServiceResult> LogoutAsync(Guid userId)
        {
            var tokens = await _identityContext.AccessTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTimeOffset.UtcNow;
            }

            await _identityContext.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> RevokeAllTokensAsync(Guid userId)
        {
            var accessTokens = await _identityContext.AccessTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            var refreshTokens = await _identityContext.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in accessTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTimeOffset.UtcNow;
            }

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTimeOffset.UtcNow;
            }

            await _identityContext.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult<UserDTO>> GetByIdAsync(Guid userId)
        {
            var user = await _identityContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult<UserDTO>.Fail("User not found");

            return ServiceResult<UserDTO>.Ok(MapToUserDTO(user));
        }

        public async Task<ServiceResult<UserDTO>> GetByUsernameAsync(string username)
        {
            var user = await _identityContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return ServiceResult<UserDTO>.Fail("User not found");

            return ServiceResult<UserDTO>.Ok(MapToUserDTO(user));
        }

        public async Task<ServiceResult<UserProfileDTO>> GetProfileAsync(Guid userId)
        {
            var user = await _identityContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult<UserProfileDTO>.Fail("User not found");

            var membership = await _applicationContext.Memberships
                .FirstOrDefaultAsync(m => m.UserId == userId);

            var profile = new UserProfileDTO
            {
                Id = user.Id,
                Username = user.Username,
                CreatedAt = user.CreatedAt,
                IsActiveMember = membership?.IsActive ?? false,
                IsInGracePeriod = membership?.IsInGracePeriod ?? false,
                GracePeriodMonthsRemaining = membership != null
                    ? membership.GracePeriodMonthsEarned - membership.GracePeriodMonthsUsed
                    : null,
                MembershipExpiresAt = membership?.ExpiresAt,
                RoleNames = user.UserRoles.Select(ur => ur.Role.RoleName.ToString()).ToList()
            };

            return ServiceResult<UserProfileDTO>.Ok(profile);
        }

        public async Task<ServiceResult<List<UserDTO>>> GetAllUsersAsync()
        {
            var users = await _identityContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();

            return ServiceResult<List<UserDTO>>.Ok(users.Select(MapToUserDTO).ToList());
        }

        public async Task<ServiceResult> AssignRoleAsync(Guid userId, RoleTypeDTO role)
        {
            var user = await _identityContext.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult.Fail("User not found");

            var dbRole = await _identityContext.Roles
                .FirstOrDefaultAsync(r => r.RoleName == (DbRoleType)(int)role);
            if (dbRole == null)
                return ServiceResult.Fail("Role not found");

            var existingAssignment = await _identityContext.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == dbRole.Id);
            if (existingAssignment != null)
                return ServiceResult.Fail("User already has this role");

            _identityContext.UserRoles.Add(new UserRoleDB
            {
                UserId = userId,
                RoleId = dbRole.Id,
                AssignedAt = DateTimeOffset.UtcNow
            });

            await _identityContext.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> RemoveRoleAsync(Guid userId, RoleTypeDTO role)
        {
            var dbRole = await _identityContext.Roles
                .FirstOrDefaultAsync(r => r.RoleName == (DbRoleType)(int)role);
            if (dbRole == null)
                return ServiceResult.Fail("Role not found");

            var assignment = await _identityContext.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == dbRole.Id);
            if (assignment == null)
                return ServiceResult.Fail("User does not have this role");

            _identityContext.UserRoles.Remove(assignment);
            await _identityContext.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult<List<RoleDTO>>> GetUserRolesAsync(Guid userId)
        {
            var roles = await _identityContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .Select(ur => new RoleDTO
                {
                    Id = ur.Role.Id,
                    RoleName = (RoleTypeDTO)(int)ur.Role.RoleName
                })
                .ToListAsync();

            return ServiceResult<List<RoleDTO>>.Ok(roles);
        }

        public async Task<ServiceResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _identityContext.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult.Fail("User not found");

            if (!PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
                return ServiceResult.Fail("Current password is incorrect");

            if (newPassword.Length < 8)
                return ServiceResult.Fail("New password must be at least 8 characters");

            var newSalt = PasswordHelper.GenerateSalt();
            user.PasswordHash = PasswordHelper.HashPassword(newPassword, newSalt);
            user.PasswordSalt = newSalt;

            await _identityContext.SaveChangesAsync();
            await RevokeAllTokensAsync(userId);

            return ServiceResult.Ok();
        }

        private static UserDTO MapToUserDTO(UserDB user)
        {
            return new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles?.Select(ur => new RoleDTO
                {
                    Id = ur.Role.Id,
                    RoleName = (RoleTypeDTO)(int)ur.Role.RoleName
                }).ToList()
            };
        }

        private static string GenerateSecureToken()
        {
            var tokenBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes);
        }
    }
}
