using mk8.identity.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace mk8.identity.Contracts.Interfaces
{
    public interface IPrivilegesService
    {
        Task<ServiceResult<PrivilegesDTO>> GetPrivilegesForUserAsync(Guid userId);
        Task<ServiceResult> GrantVotingRightsAsync(Guid userId);
        Task<ServiceResult> RevokeVotingRightsAsync(Guid userId);
        Task<ServiceResult<bool>> HasVotingRightsAsync(Guid userId);
    }
}
