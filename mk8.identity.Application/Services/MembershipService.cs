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
    public class MembershipService : IMembershipService
    {
        private readonly ApplicationContext _applicationContext;
        private readonly IdentityContext _identityContext;
        private readonly INotificationService _notificationService;
        private readonly IMatrixAccountService _matrixAccountService;

        private const int BaseGracePeriodMonths = 1;
        private const int MaxGracePeriodMonths = 24;

        public MembershipService(
            ApplicationContext applicationContext,
            IdentityContext identityContext,
            INotificationService notificationService,
            IMatrixAccountService matrixAccountService)
        {
            _applicationContext = applicationContext;
            _identityContext = identityContext;
            _notificationService = notificationService;
            _matrixAccountService = matrixAccountService;
        }

        public async Task<ServiceResult<MembershipStatusDTO>> GetMembershipStatusAsync(Guid userId)
        {
            var user = await _identityContext.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult<MembershipStatusDTO>.Fail("User not found");

            var membership = await _applicationContext.Memberships
                .Include(m => m.Contributions)
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership == null)
                return ServiceResult<MembershipStatusDTO>.Fail("Membership not found");

            var lastContribution = membership.Contributions
                .Where(c => c.Status == ContributionStatusDB.Validated || c.Status == ContributionStatusDB.AutoVerified)
                .OrderByDescending(c => c.ContributionPeriodEnd)
                .FirstOrDefault();

            return ServiceResult<MembershipStatusDTO>.Ok(new MembershipStatusDTO
            {
                UserId = userId,
                Username = user.Username,
                IsActive = membership.IsActive,
                IsInGracePeriod = membership.IsInGracePeriod,
                StartDate = membership.StartDate,
                GracePeriodMonthsEarned = membership.GracePeriodMonthsEarned,
                GracePeriodMonthsRemaining = membership.GracePeriodMonthsEarned - membership.GracePeriodMonthsUsed,
                LastContributionDate = lastContribution?.ContributionPeriodEnd,
                ExpiresAt = membership.ExpiresAt,
                TotalYearsAsMember = (int)((DateTimeOffset.UtcNow - membership.StartDate).TotalDays / 365.25)
            });
        }

        public async Task<ServiceResult<UserMembershipDTO>> GetMembershipDetailsAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .Include(m => m.Contributions)
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership == null)
                return ServiceResult<UserMembershipDTO>.Fail("Membership not found");

            var contributions = new List<ContributionDTO>();
            foreach (var c in membership.Contributions.OrderByDescending(c => c.Year).ThenByDescending(c => c.Month))
            {
                contributions.Add(new ContributionDTO
                {
                    Id = c.Id,
                    Type = (ContributionTypeDTO)(int)c.Type,
                    Status = (ContributionStatusDTO)(int)c.Status,
                    SubmittedAt = c.SubmittedAt,
                    ContributionPeriodStart = c.ContributionPeriodStart,
                    ContributionPeriodEnd = c.ContributionPeriodEnd,
                    Month = (int)c.Month,
                    Year = c.Year,
                    Description = c.Description
                });
            }

            return ServiceResult<UserMembershipDTO>.Ok(new UserMembershipDTO
            {
                Id = membership.Id,
                IsActive = membership.IsActive,
                StartDate = membership.StartDate,
                IsInGracePeriod = membership.IsInGracePeriod,
                GracePeriodStartedAt = membership.GracePeriodStartedAt,
                GracePeriodMonthsEarned = membership.GracePeriodMonthsEarned,
                GracePeriodMonthsUsed = membership.GracePeriodMonthsUsed,
                ExpiresAt = membership.ExpiresAt,
                Contributions = contributions
            });
        }

        public async Task<ServiceResult<bool>> IsActiveMemberAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .FirstOrDefaultAsync(m => m.UserId == userId);

            return ServiceResult<bool>.Ok(membership?.IsActive ?? false);
        }

        public async Task<ServiceResult<List<MembershipCheckResultDTO>>> RunDailyMembershipCheckAsync()
        {
            var results = new List<MembershipCheckResultDTO>();
            var now = DateTimeOffset.UtcNow;

            var memberships = await _applicationContext.Memberships
                .Include(m => m.Contributions)
                .ToListAsync();

            foreach (var membership in memberships)
            {
                var user = await _identityContext.Users.FindAsync(membership.UserId);
                if (user == null) continue;

                var wasActive = membership.IsActive;
                var wasInGracePeriod = membership.IsInGracePeriod;

                var lastContribution = membership.Contributions
                    .Where(c => c.Status == ContributionStatusDB.Validated || c.Status == ContributionStatusDB.AutoVerified)
                    .OrderByDescending(c => c.ContributionPeriodEnd)
                    .FirstOrDefault();

                if (lastContribution == null)
                {
                    if (wasActive)
                    {
                        membership.IsActive = false;
                        membership.DeactivationDates.Add(now);
                        await _notificationService.CreateMembershipDeactivatedNotificationAsync(user.Id, user.Username);
                    }
                    continue;
                }

                var baseExpiry = lastContribution.ContributionPeriodEnd.AddMonths(1);
                var yearsAsMember = (int)((now - membership.StartDate).TotalDays / 365.25);
                var gracePeriodMonths = Math.Min(yearsAsMember + BaseGracePeriodMonths, MaxGracePeriodMonths);
                membership.GracePeriodMonthsEarned = gracePeriodMonths;
                var finalExpiry = baseExpiry.AddMonths(gracePeriodMonths);
                membership.ExpiresAt = finalExpiry;

                var enteredGracePeriod = false;
                var wasDeactivated = false;

                if (now > baseExpiry && now <= finalExpiry)
                {
                    // In grace period
                    if (!membership.IsInGracePeriod)
                    {
                        membership.IsInGracePeriod = true;
                        membership.GracePeriodStartedAt = now;
                        membership.GracePeriodMonthsUsed = 1;
                        enteredGracePeriod = true;

                        await _notificationService.CreateGracePeriodNotificationAsync(
                            user.Id, user.Username, 1, gracePeriodMonths);
                    }
                    else
                    {
                        // Monthly reminder
                        var monthsInGrace = (int)((now - membership.GracePeriodStartedAt!.Value).TotalDays / 30);
                        if (monthsInGrace > membership.GracePeriodMonthsUsed)
                        {
                            membership.GracePeriodMonthsUsed = monthsInGrace;
                            var remaining = gracePeriodMonths - monthsInGrace;
                            await _notificationService.CreateGracePeriodNotificationAsync(
                                user.Id, user.Username, monthsInGrace, remaining);
                        }
                    }
                }
                else if (now > finalExpiry && membership.IsActive)
                {
                    // Deactivate membership
                    membership.IsActive = false;
                    membership.IsInGracePeriod = false;
                    membership.DeactivationDates.Add(now);
                    wasDeactivated = true;

                    await _notificationService.CreateMembershipDeactivatedNotificationAsync(user.Id, user.Username);

                    // Check for Matrix accounts to disable
                    var matrixAccounts = await _matrixAccountService.GetMatrixAccountsForUserAsync(user.Id);
                    if (matrixAccounts.Success && matrixAccounts.Data != null)
                    {
                        foreach (var account in matrixAccounts.Data.Where(m => !m.IsDisabled))
                        {
                            await _notificationService.CreateMatrixDisableRequiredNotificationAsync(
                                user.Id, user.Username, account.Username);
                        }
                    }
                }

                results.Add(new MembershipCheckResultDTO
                {
                    UserId = user.Id,
                    Username = user.Username,
                    WasActive = wasActive,
                    IsNowActive = membership.IsActive,
                    EnteredGracePeriod = enteredGracePeriod,
                    WasDeactivated = wasDeactivated,
                    GracePeriodMonth = membership.GracePeriodMonthsUsed,
                    GracePeriodMonthsRemaining = gracePeriodMonths - membership.GracePeriodMonthsUsed
                });
            }

            await _applicationContext.SaveChangesAsync();
            return ServiceResult<List<MembershipCheckResultDTO>>.Ok(results);
        }

        public async Task<ServiceResult> ActivateMembershipAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership == null)
                return ServiceResult.Fail("Membership not found");

            if (membership.IsActive)
                return ServiceResult.Fail("Membership is already active");

            membership.IsActive = true;
            membership.ActivationDates.Add(DateTimeOffset.UtcNow);
            membership.IsInGracePeriod = false;
            membership.GracePeriodStartedAt = null;
            membership.GracePeriodMonthsUsed = 0;

            await _applicationContext.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> DeactivateMembershipAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership == null)
                return ServiceResult.Fail("Membership not found");

            if (!membership.IsActive)
                return ServiceResult.Fail("Membership is already inactive");

            var user = await _identityContext.Users.FindAsync(userId);

            membership.IsActive = false;
            membership.DeactivationDates.Add(DateTimeOffset.UtcNow);
            membership.IsInGracePeriod = false;

            await _applicationContext.SaveChangesAsync();

            if (user != null)
            {
                await _notificationService.CreateMembershipDeactivatedNotificationAsync(user.Id, user.Username);
            }

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult<int>> CalculateGracePeriodMonthsAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships
                .FirstOrDefaultAsync(m => m.UserId == userId);

            if (membership == null)
                return ServiceResult<int>.Fail("Membership not found");

            var yearsAsMember = (int)((DateTimeOffset.UtcNow - membership.StartDate).TotalDays / 365.25);
            var gracePeriodMonths = Math.Min(yearsAsMember + BaseGracePeriodMonths, MaxGracePeriodMonths);

            return ServiceResult<int>.Ok(gracePeriodMonths);
        }

        public async Task<ServiceResult<List<MembershipStatusDTO>>> GetMembersInGracePeriodAsync()
        {
            var memberships = await _applicationContext.Memberships
                .Where(m => m.IsInGracePeriod)
                .ToListAsync();

            var result = new List<MembershipStatusDTO>();
            foreach (var membership in memberships)
            {
                var user = await _identityContext.Users.FindAsync(membership.UserId);
                if (user == null) continue;

                result.Add(new MembershipStatusDTO
                {
                    UserId = membership.UserId,
                    Username = user.Username,
                    IsActive = membership.IsActive,
                    IsInGracePeriod = membership.IsInGracePeriod,
                    StartDate = membership.StartDate,
                    GracePeriodMonthsEarned = membership.GracePeriodMonthsEarned,
                    GracePeriodMonthsRemaining = membership.GracePeriodMonthsEarned - membership.GracePeriodMonthsUsed,
                    ExpiresAt = membership.ExpiresAt,
                    TotalYearsAsMember = (int)((DateTimeOffset.UtcNow - membership.StartDate).TotalDays / 365.25)
                });
            }

            return ServiceResult<List<MembershipStatusDTO>>.Ok(result);
        }

        public async Task<ServiceResult<List<MembershipStatusDTO>>> GetMembersExpiringWithinDaysAsync(int days)
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(days);

            var memberships = await _applicationContext.Memberships
                .Where(m => m.IsActive && m.ExpiresAt.HasValue && m.ExpiresAt.Value <= cutoffDate)
                .ToListAsync();

            var result = new List<MembershipStatusDTO>();
            foreach (var membership in memberships)
            {
                var user = await _identityContext.Users.FindAsync(membership.UserId);
                if (user == null) continue;

                result.Add(new MembershipStatusDTO
                {
                    UserId = membership.UserId,
                    Username = user.Username,
                    IsActive = membership.IsActive,
                    IsInGracePeriod = membership.IsInGracePeriod,
                    StartDate = membership.StartDate,
                    GracePeriodMonthsEarned = membership.GracePeriodMonthsEarned,
                    GracePeriodMonthsRemaining = membership.GracePeriodMonthsEarned - membership.GracePeriodMonthsUsed,
                    ExpiresAt = membership.ExpiresAt,
                    TotalYearsAsMember = (int)((DateTimeOffset.UtcNow - membership.StartDate).TotalDays / 365.25)
                });
            }

            return ServiceResult<List<MembershipStatusDTO>>.Ok(result);
        }

        public async Task<ServiceResult<List<MembershipStatusDTO>>> GetAllActiveMembersAsync()
        {
            var memberships = await _applicationContext.Memberships
                .Where(m => m.IsActive)
                .ToListAsync();

            var result = new List<MembershipStatusDTO>();
            foreach (var membership in memberships)
            {
                var user = await _identityContext.Users.FindAsync(membership.UserId);
                if (user == null) continue;

                result.Add(new MembershipStatusDTO
                {
                    UserId = membership.UserId,
                    Username = user.Username,
                    IsActive = membership.IsActive,
                    IsInGracePeriod = membership.IsInGracePeriod,
                    StartDate = membership.StartDate,
                    GracePeriodMonthsEarned = membership.GracePeriodMonthsEarned,
                    GracePeriodMonthsRemaining = membership.GracePeriodMonthsEarned - membership.GracePeriodMonthsUsed,
                    ExpiresAt = membership.ExpiresAt,
                    TotalYearsAsMember = (int)((DateTimeOffset.UtcNow - membership.StartDate).TotalDays / 365.25)
                });
            }

            return ServiceResult<List<MembershipStatusDTO>>.Ok(result);
        }

        public async Task<ServiceResult<List<MembershipStatusDTO>>> GetAllInactiveMembersAsync()
        {
            var memberships = await _applicationContext.Memberships
                .Where(m => !m.IsActive)
                .ToListAsync();

            var result = new List<MembershipStatusDTO>();
            foreach (var membership in memberships)
            {
                var user = await _identityContext.Users.FindAsync(membership.UserId);
                if (user == null) continue;

                result.Add(new MembershipStatusDTO
                {
                    UserId = membership.UserId,
                    Username = user.Username,
                    IsActive = membership.IsActive,
                    IsInGracePeriod = membership.IsInGracePeriod,
                    StartDate = membership.StartDate,
                    GracePeriodMonthsEarned = membership.GracePeriodMonthsEarned,
                    GracePeriodMonthsRemaining = membership.GracePeriodMonthsEarned - membership.GracePeriodMonthsUsed,
                    ExpiresAt = membership.ExpiresAt,
                    TotalYearsAsMember = (int)((DateTimeOffset.UtcNow - membership.StartDate).TotalDays / 365.25)
                });
            }

            return ServiceResult<List<MembershipStatusDTO>>.Ok(result);
        }
    }
}
