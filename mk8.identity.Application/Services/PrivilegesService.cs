using Microsoft.EntityFrameworkCore;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using mk8.identity.Infrastructure.Contexts;
using mk8.identity.Infrastructure.Models.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mk8.identity.Application.Services
{
    public class PrivilegesService : IPrivilegesService
    {
        private readonly ApplicationContext _applicationContext;
        private readonly IdentityContext _identityContext;
        private readonly IMatrixAccountService _matrixAccountService;

        public PrivilegesService(
            ApplicationContext applicationContext,
            IdentityContext identityContext,
            IMatrixAccountService matrixAccountService)
        {
            _applicationContext = applicationContext;
            _identityContext = identityContext;
            _matrixAccountService = matrixAccountService;
        }

        public async Task<ServiceResult<PrivilegesDTO>> GetPrivilegesForUserAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .Include(m => m.Privileges)
                    .ThenInclude(p => p!.MatrixAccounts)
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership == null)
                return ServiceResult<PrivilegesDTO>.Fail("User membership not found");

            if (membership.Privileges == null)
            {
                membership.Privileges = new PrivilegesDB
                {
                    Id = Guid.NewGuid(),
                    MembershipId = membership.Id,
                    VotingRights = false
                };
                await _applicationContext.SaveChangesAsync();
            }

            var user = await _identityContext.Users.FindAsync(userId);

            var matrixAccounts = membership.Privileges.MatrixAccounts.Select(m => new MatrixAccountDTO
            {
                Id = m.Id,
                AccountId = m.AccountId,
                Username = m.Username,
                CreatedAt = m.CreatedAt,
                IsDisabled = m.IsDisabled,
                DisabledAt = m.DisabledAt,
                OwnerId = userId,
                OwnerUsername = user?.Username ?? "Unknown"
            }).ToList();

            return ServiceResult<PrivilegesDTO>.Ok(new PrivilegesDTO
            {
                Id = membership.Privileges.Id,
                UserId = userId,
                MatrixAccounts = matrixAccounts,
                VotingRights = membership.Privileges.VotingRights
            });
        }

        public async Task<ServiceResult> GrantVotingRightsAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .Include(m => m.Privileges)
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership == null)
                return ServiceResult.Fail("User membership not found");

            if (membership.Privileges == null)
            {
                membership.Privileges = new PrivilegesDB
                {
                    Id = Guid.NewGuid(),
                    MembershipId = membership.Id,
                    VotingRights = true
                };
            }
            else
            {
                membership.Privileges.VotingRights = true;
            }

            await _applicationContext.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> RevokeVotingRightsAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .Include(m => m.Privileges)
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership?.Privileges == null)
                return ServiceResult.Fail("User privileges not found");

            membership.Privileges.VotingRights = false;
            await _applicationContext.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult<bool>> HasVotingRightsAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .Include(m => m.Privileges)
                .FirstOrDefaultAsync(m => m.UserId == userId);

            return ServiceResult<bool>.Ok(membership?.Privileges?.VotingRights ?? false);
        }
    }
}
