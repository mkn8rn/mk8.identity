using mk8.identity.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace mk8.identity.Contracts.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResult<UserDTO>> RegisterAsync(UserRegistrationDTO registration);
        Task<ServiceResult<AuthTokensDTO>> LoginAsync(UserLoginDTO login);
        Task<ServiceResult<AuthTokensDTO>> RefreshTokenAsync(string refreshToken);
        Task<ServiceResult> LogoutAsync(Guid userId);
        Task<ServiceResult> RevokeAllTokensAsync(Guid userId);

        Task<ServiceResult<UserDTO>> GetByIdAsync(Guid userId);
        Task<ServiceResult<UserDTO>> GetByUsernameAsync(string username);
        Task<ServiceResult<UserProfileDTO>> GetProfileAsync(Guid userId);
        Task<ServiceResult<List<UserDTO>>> GetAllUsersAsync();

        Task<ServiceResult> AssignRoleAsync(Guid userId, RoleTypeDTO role);
        Task<ServiceResult> RemoveRoleAsync(Guid userId, RoleTypeDTO role);
        Task<ServiceResult<List<RoleDTO>>> GetUserRolesAsync(Guid userId);

        Task<ServiceResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    }
}
