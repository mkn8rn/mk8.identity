using Microsoft.EntityFrameworkCore;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using mk8.identity.Infrastructure.Contexts;
using mk8.identity.Infrastructure.Models.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mk8.identity.Application.Services
{
    public class MatrixAccountService : IMatrixAccountService
    {
        private readonly ApplicationContext _applicationContext;
        private readonly IdentityContext _identityContext;
        private readonly INotificationService _notificationService;

        public MatrixAccountService(
            ApplicationContext applicationContext,
            IdentityContext identityContext,
            INotificationService notificationService)
        {
            _applicationContext = applicationContext;
            _identityContext = identityContext;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<MatrixAccountDTO>> CreateMatrixAccountAsync(Guid adminId, MatrixAccountCreateDTO account)
        {
            var ownerMembership = await _applicationContext.Memberships
                .Include(m => m.Privileges)
                .FirstOrDefaultAsync(m => m.UserId == account.OwnerId);

            if (ownerMembership == null)
                return ServiceResult<MatrixAccountDTO>.Fail("Owner membership not found");

            var adminMembership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == adminId);
            if (adminMembership == null)
                return ServiceResult<MatrixAccountDTO>.Fail("Admin membership not found");

            if (ownerMembership.Privileges == null)
            {
                ownerMembership.Privileges = new PrivilegesDB
                {
                    Id = Guid.NewGuid(),
                    MembershipId = ownerMembership.Id,
                    VotingRights = false
                };
                await _applicationContext.SaveChangesAsync();
            }

            var existingAccount = await _applicationContext.MatrixAccounts
                .FirstOrDefaultAsync(m => m.Username == account.Username);
            if (existingAccount != null)
                return ServiceResult<MatrixAccountDTO>.Fail("Matrix username already exists");

            var matrixAccount = new MatrixAccountDB
            {
                Id = Guid.NewGuid(),
                AccountId = $"@{account.Username}:matrix.example.org",
                Username = account.Username,
                CreatedAt = DateTimeOffset.UtcNow,
                IsDisabled = false,
                PrivilegesId = ownerMembership.Privileges.Id,
                CreatedByMembershipId = adminMembership.Id
            };

            _applicationContext.MatrixAccounts.Add(matrixAccount);
            await _applicationContext.SaveChangesAsync();

            var owner = await _identityContext.Users.FindAsync(account.OwnerId);

            return ServiceResult<MatrixAccountDTO>.Ok(new MatrixAccountDTO
            {
                Id = matrixAccount.Id,
                AccountId = matrixAccount.AccountId,
                Username = matrixAccount.Username,
                CreatedAt = matrixAccount.CreatedAt,
                IsDisabled = matrixAccount.IsDisabled,
                OwnerId = account.OwnerId,
                OwnerUsername = owner?.Username ?? "Unknown"
            });
        }

        public async Task<ServiceResult> DisableMatrixAccountAsync(Guid adminId, MatrixAccountDisableDTO disable)
        {
            var matrixAccount = await _applicationContext.MatrixAccounts
                .Include(m => m.Privileges)
                    .ThenInclude(p => p.Membership)
                .FirstOrDefaultAsync(m => m.Id == disable.MatrixAccountId);

            if (matrixAccount == null)
                return ServiceResult.Fail("Matrix account not found");

            var adminMembership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == adminId);
            if (adminMembership == null)
                return ServiceResult.Fail("Admin membership not found");

            matrixAccount.IsDisabled = true;
            matrixAccount.DisabledAt = DateTimeOffset.UtcNow;
            matrixAccount.DisabledByMembershipId = adminMembership.Id;

            await _applicationContext.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> EnableMatrixAccountAsync(Guid adminId, Guid matrixAccountId)
        {
            var matrixAccount = await _applicationContext.MatrixAccounts
                .Include(m => m.Privileges)
                    .ThenInclude(p => p.Membership)
                .FirstOrDefaultAsync(m => m.Id == matrixAccountId);

            if (matrixAccount == null)
                return ServiceResult.Fail("Matrix account not found");

            var ownerUserId = matrixAccount.Privileges.Membership.UserId;
            var ownerMembership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == ownerUserId);

            if (ownerMembership == null || !ownerMembership.IsActive)
                return ServiceResult.Fail("Cannot enable Matrix account for inactive member");

            matrixAccount.IsDisabled = false;
            matrixAccount.DisabledAt = null;
            matrixAccount.DisabledByMembershipId = null;

            await _applicationContext.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult<MatrixAccountDTO>> GetByIdAsync(Guid matrixAccountId)
        {
            var matrixAccount = await _applicationContext.MatrixAccounts
                .Include(m => m.Privileges)
                    .ThenInclude(p => p.Membership)
                .FirstOrDefaultAsync(m => m.Id == matrixAccountId);

            if (matrixAccount == null)
                return ServiceResult<MatrixAccountDTO>.Fail("Matrix account not found");

            var ownerUserId = matrixAccount.Privileges.Membership.UserId;
            var owner = await _identityContext.Users.FindAsync(ownerUserId);

            return ServiceResult<MatrixAccountDTO>.Ok(new MatrixAccountDTO
            {
                Id = matrixAccount.Id,
                AccountId = matrixAccount.AccountId,
                Username = matrixAccount.Username,
                CreatedAt = matrixAccount.CreatedAt,
                IsDisabled = matrixAccount.IsDisabled,
                DisabledAt = matrixAccount.DisabledAt,
                OwnerId = ownerUserId,
                OwnerUsername = owner?.Username ?? "Unknown"
            });
        }

        public async Task<ServiceResult<List<MatrixAccountDTO>>> GetMatrixAccountsForUserAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .Include(m => m.Privileges)
                    .ThenInclude(p => p!.MatrixAccounts)
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership?.Privileges == null)
                return ServiceResult<List<MatrixAccountDTO>>.Ok([]);

            var user = await _identityContext.Users.FindAsync(userId);

            var result = membership.Privileges.MatrixAccounts.Select(m => new MatrixAccountDTO
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

            return ServiceResult<List<MatrixAccountDTO>>.Ok(result);
        }

        public async Task<ServiceResult<List<MatrixAccountDTO>>> GetAllMatrixAccountsAsync(bool? isDisabled = null)
        {
            var query = _applicationContext.MatrixAccounts
                .Include(m => m.Privileges)
                    .ThenInclude(p => p.Membership)
                .AsQueryable();

            if (isDisabled == true)
                query = query.Where(m => m.IsDisabled);
            else if (isDisabled == false)
                query = query.Where(m => !m.IsDisabled);

            var accounts = await query.ToListAsync();

            var result = new List<MatrixAccountDTO>();
            foreach (var account in accounts)
            {
                var ownerUserId = account.Privileges.Membership.UserId;
                var owner = await _identityContext.Users.FindAsync(ownerUserId);

                result.Add(new MatrixAccountDTO
                {
                    Id = account.Id,
                    AccountId = account.AccountId,
                    Username = account.Username,
                    CreatedAt = account.CreatedAt,
                    IsDisabled = account.IsDisabled,
                    DisabledAt = account.DisabledAt,
                    OwnerId = ownerUserId,
                    OwnerUsername = owner?.Username ?? "Unknown"
                });
            }

            return ServiceResult<List<MatrixAccountDTO>>.Ok(result);
        }

        public async Task<ServiceResult<List<MatrixAccountDTO>>> GetAccountsRequiringDisableAsync()
        {
            var accounts = await _applicationContext.MatrixAccounts
                .Include(m => m.Privileges)
                    .ThenInclude(p => p.Membership)
                .Where(m => !m.IsDisabled)
                .ToListAsync();

            var result = new List<MatrixAccountDTO>();
            foreach (var account in accounts)
            {
                var membership = account.Privileges.Membership;
                if (!membership.IsActive)
                {
                    var owner = await _identityContext.Users.FindAsync(membership.UserId);

                    result.Add(new MatrixAccountDTO
                    {
                        Id = account.Id,
                        AccountId = account.AccountId,
                        Username = account.Username,
                        CreatedAt = account.CreatedAt,
                        IsDisabled = account.IsDisabled,
                        OwnerId = membership.UserId,
                        OwnerUsername = owner?.Username ?? "Unknown"
                    });
                }
            }

            return ServiceResult<List<MatrixAccountDTO>>.Ok(result);
        }

        public async Task<ServiceResult<bool>> UserHasActiveMatrixAccountAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .Include(m => m.Privileges)
                    .ThenInclude(p => p!.MatrixAccounts)
                .FirstOrDefaultAsync(m => m.UserId == userId);

            var hasAccount = membership?.Privileges?.MatrixAccounts.Any(m => !m.IsDisabled) ?? false;
            return ServiceResult<bool>.Ok(hasAccount);
        }
    }
}
