using mk8.identity.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace mk8.identity.Contracts.Interfaces
{
    public interface IMembershipService
    {
        // membership status
        Task<ServiceResult<MembershipStatusDTO>> GetMembershipStatusAsync(Guid userId);
        Task<ServiceResult<UserMembershipDTO>> GetMembershipDetailsAsync(Guid userId);
        Task<ServiceResult<bool>> IsActiveMemberAsync(Guid userId);

        // daily job - checks all memberships and generates notifications
        Task<ServiceResult<List<MembershipCheckResultDTO>>> RunDailyMembershipCheckAsync();

        // manual membership operations (admin)
        Task<ServiceResult> ActivateMembershipAsync(Guid userId);
        Task<ServiceResult> DeactivateMembershipAsync(Guid userId);

        // grace period
        Task<ServiceResult<int>> CalculateGracePeriodMonthsAsync(Guid userId);
        Task<ServiceResult<List<MembershipStatusDTO>>> GetMembersInGracePeriodAsync();
        Task<ServiceResult<List<MembershipStatusDTO>>> GetMembersExpiringWithinDaysAsync(int days);

        // queries
        Task<ServiceResult<List<MembershipStatusDTO>>> GetAllActiveMembersAsync();
        Task<ServiceResult<List<MembershipStatusDTO>>> GetAllInactiveMembersAsync();
    }
}
