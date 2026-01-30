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
    public class ContributionService : IContributionService
    {
        private readonly ApplicationContext _applicationContext;
        private readonly IdentityContext _identityContext;
        private readonly INotificationService _notificationService;

        public ContributionService(
            ApplicationContext applicationContext,
            IdentityContext identityContext,
            INotificationService notificationService)
        {
            _applicationContext = applicationContext;
            _identityContext = identityContext;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<ContributionDTO>> SubmitContributionAsync(Guid userId, ContributionSubmitDTO contribution)
        {
            if (contribution.Type == ContributionTypeDTO.Invalid)
                return ServiceResult<ContributionDTO>.Fail("Invalid contribution type");

            if (!contribution.Type.IsMemberSubmittable())
                return ServiceResult<ContributionDTO>.Fail("This contribution type cannot be manually submitted. It is assigned automatically.");

            if (contribution.Month < 1 || contribution.Month > 12)
                return ServiceResult<ContributionDTO>.Fail("Invalid month");

            var user = await _identityContext.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult<ContributionDTO>.Fail("User not found");

            var membership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == userId);
            if (membership == null)
                return ServiceResult<ContributionDTO>.Fail("User membership not found");

            var existingContribution = await _applicationContext.Contributions
                .FirstOrDefaultAsync(c => c.MembershipId == membership.Id
                    && c.Month == (MonthType)contribution.Month
                    && c.Year == contribution.Year
                    && c.Type == (ContributionTypeDB)(int)contribution.Type);

            if (existingContribution != null)
                return ServiceResult<ContributionDTO>.Fail("Contribution for this period and type already exists");

            var periodStart = new DateTimeOffset(new DateTime(contribution.Year, contribution.Month, 1), TimeSpan.Zero);
            var periodEnd = periodStart.AddMonths(1).AddSeconds(-1);

            var dbContribution = new ContributionDB
            {
                Id = Guid.NewGuid(),
                Type = (ContributionTypeDB)(int)contribution.Type,
                Status = ContributionStatusDB.Pending,
                SubmittedAt = DateTimeOffset.UtcNow,
                ContributionPeriodStart = periodStart,
                ContributionPeriodEnd = periodEnd,
                Month = (MonthType)contribution.Month,
                Year = contribution.Year,
                MembershipId = membership.Id,
                SubmittedByMembershipId = membership.Id,
                Description = contribution.Description
            };

            _applicationContext.Contributions.Add(dbContribution);
            await _applicationContext.SaveChangesAsync();

            await _notificationService.CreateContributionSubmittedNotificationAsync(userId, user.Username, contribution.Type);

            return ServiceResult<ContributionDTO>.Ok(await MapToContributionDTOAsync(dbContribution));
        }

        public async Task<ServiceResult<List<ContributionDTO>>> GetUserContributionsAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == userId);
            if (membership == null)
                return ServiceResult<List<ContributionDTO>>.Fail("User membership not found");

            var contributions = await _applicationContext.Contributions
                .Where(c => c.MembershipId == membership.Id)
                .OrderByDescending(c => c.Year)
                .ThenByDescending(c => c.Month)
                .ToListAsync();

            var result = new List<ContributionDTO>();
            foreach (var contribution in contributions)
            {
                result.Add(await MapToContributionDTOAsync(contribution));
            }

            return ServiceResult<List<ContributionDTO>>.Ok(result);
        }

        public async Task<ServiceResult<ContributionDTO>> CreateAndValidateContributionAsync(Guid assessorId, ContributionCreateByAssessorDTO contribution)
        {
            var targetMembership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == contribution.UserId);
            if (targetMembership == null)
                return ServiceResult<ContributionDTO>.Fail("Target user membership not found");

            var assessorMembership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == assessorId);
            if (assessorMembership == null)
                return ServiceResult<ContributionDTO>.Fail("Assessor membership not found");

            var periodStart = new DateTimeOffset(new DateTime(contribution.Year, contribution.Month, 1), TimeSpan.Zero);
            var periodEnd = periodStart.AddMonths(1).AddSeconds(-1);

            var dbContribution = new ContributionDB
            {
                Id = Guid.NewGuid(),
                Type = (ContributionTypeDB)(int)contribution.Type,
                Status = ContributionStatusDB.Validated,
                SubmittedAt = DateTimeOffset.UtcNow,
                ContributionPeriodStart = periodStart,
                ContributionPeriodEnd = periodEnd,
                Month = (MonthType)contribution.Month,
                Year = contribution.Year,
                MembershipId = targetMembership.Id,
                SubmittedByMembershipId = assessorMembership.Id,
                ValidatedByMembershipId = assessorMembership.Id,
                ValidatedAt = DateTimeOffset.UtcNow,
                Description = contribution.Description
            };

            _applicationContext.Contributions.Add(dbContribution);
            await UpdateMembershipStatusAsync(targetMembership);
            await _applicationContext.SaveChangesAsync();

            return ServiceResult<ContributionDTO>.Ok(await MapToContributionDTOAsync(dbContribution));
        }

        public async Task<ServiceResult<ContributionDTO>> ValidateContributionAsync(Guid assessorId, ContributionValidateDTO validation)
        {
            var contribution = await _applicationContext.Contributions
                .Include(c => c.Membership)
                .FirstOrDefaultAsync(c => c.Id == validation.ContributionId);

            if (contribution == null)
                return ServiceResult<ContributionDTO>.Fail("Contribution not found");

            var assessorMembership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == assessorId);
            if (assessorMembership == null)
                return ServiceResult<ContributionDTO>.Fail("Assessor membership not found");

            contribution.Status = validation.Approved ? ContributionStatusDB.Validated : ContributionStatusDB.Rejected;
            contribution.ValidatedByMembershipId = assessorMembership.Id;
            contribution.ValidatedAt = DateTimeOffset.UtcNow;

            if (validation.Approved)
            {
                await UpdateMembershipStatusAsync(contribution.Membership);
            }

            await _applicationContext.SaveChangesAsync();

            return ServiceResult<ContributionDTO>.Ok(await MapToContributionDTOAsync(contribution));
        }

        public async Task<ServiceResult<List<ContributionDTO>>> GetPendingContributionsAsync()
        {
            var contributions = await _applicationContext.Contributions
                .Where(c => c.Status == ContributionStatusDB.Pending)
                .OrderBy(c => c.SubmittedAt)
                .ToListAsync();

            var result = new List<ContributionDTO>();
            foreach (var contribution in contributions)
            {
                result.Add(await MapToContributionDTOAsync(contribution));
            }

            return ServiceResult<List<ContributionDTO>>.Ok(result);
        }

        public async Task<ServiceResult<List<ContributionDTO>>> GetContributionsByMonthAsync(int month, long year)
        {
            var contributions = await _applicationContext.Contributions
                .Where(c => c.Month == (MonthType)month && c.Year == (int)year)
                .OrderByDescending(c => c.SubmittedAt)
                .ToListAsync();

            var result = new List<ContributionDTO>();
            foreach (var contribution in contributions)
            {
                result.Add(await MapToContributionDTOAsync(contribution));
            }

            return ServiceResult<List<ContributionDTO>>.Ok(result);
        }

        public async Task<ServiceResult<List<ContributionDTO>>> ProcessGitHubSubscriptionsAsync()
        {
            await Task.CompletedTask;
            return ServiceResult<List<ContributionDTO>>.Ok([]);
        }

        public async Task<ServiceResult<ContributionDTO>> AutoVerifyContributionAsync(Guid userId, ContributionTypeDTO type, int month, long year, string externalReference)
        {
            var membership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == userId);
            if (membership == null)
                return ServiceResult<ContributionDTO>.Fail("User membership not found");

            var periodStart = new DateTimeOffset(new DateTime((int)year, month, 1), TimeSpan.Zero);
            var periodEnd = periodStart.AddMonths(1).AddSeconds(-1);

            var dbContribution = new ContributionDB
            {
                Id = Guid.NewGuid(),
                Type = (ContributionTypeDB)(int)type,
                Status = ContributionStatusDB.AutoVerified,
                SubmittedAt = DateTimeOffset.UtcNow,
                ContributionPeriodStart = periodStart,
                ContributionPeriodEnd = periodEnd,
                Month = (MonthType)month,
                Year = (int)year,
                MembershipId = membership.Id,
                SubmittedByMembershipId = membership.Id,
                ExternalReference = externalReference
            };

            _applicationContext.Contributions.Add(dbContribution);
            await UpdateMembershipStatusAsync(membership);
            await _applicationContext.SaveChangesAsync();

            return ServiceResult<ContributionDTO>.Ok(await MapToContributionDTOAsync(dbContribution));
        }

        public async Task<ServiceResult<ContributionDTO>> GetByIdAsync(Guid contributionId)
        {
            var contribution = await _applicationContext.Contributions.FindAsync(contributionId);
            if (contribution == null)
                return ServiceResult<ContributionDTO>.Fail("Contribution not found");

            return ServiceResult<ContributionDTO>.Ok(await MapToContributionDTOAsync(contribution));
        }

        public async Task<ServiceResult<List<ContributionDTO>>> GetAllContributionsAsync(int? month = null, long? year = null, ContributionStatusDTO? status = null)
        {
            var query = _applicationContext.Contributions.AsQueryable();

            if (month.HasValue)
                query = query.Where(c => c.Month == (MonthType)month.Value);
            if (year.HasValue)
                query = query.Where(c => c.Year == (int)year.Value);
            if (status.HasValue)
                query = query.Where(c => c.Status == (ContributionStatusDB)(int)status.Value);

            var contributions = await query
                .OrderByDescending(c => c.Year)
                .ThenByDescending(c => c.Month)
                .ThenByDescending(c => c.SubmittedAt)
                .ToListAsync();

            var result = new List<ContributionDTO>();
            foreach (var contribution in contributions)
            {
                result.Add(await MapToContributionDTOAsync(contribution));
            }

            return ServiceResult<List<ContributionDTO>>.Ok(result);
        }

        private async Task UpdateMembershipStatusAsync(UserMembershipDB membership)
        {
            var validContributions = await _applicationContext.Contributions
                .Where(c => c.MembershipId == membership.Id
                    && (c.Status == ContributionStatusDB.Validated || c.Status == ContributionStatusDB.AutoVerified))
                .OrderByDescending(c => c.ContributionPeriodEnd)
                .ToListAsync();

            if (validContributions.Count > 0)
            {
                var lastContribution = validContributions.First();
                var baseExpiry = lastContribution.ContributionPeriodEnd.AddMonths(1);

                var yearsAsMember = (int)((DateTimeOffset.UtcNow - membership.StartDate).TotalDays / 365.25);
                membership.GracePeriodMonthsEarned = Math.Min(yearsAsMember + 1, 24);
                membership.ExpiresAt = baseExpiry.AddMonths(membership.GracePeriodMonthsEarned);

                if (!membership.IsActive)
                {
                    membership.IsActive = true;
                    membership.ActivationDates.Add(DateTimeOffset.UtcNow);
                    membership.IsInGracePeriod = false;
                    membership.GracePeriodStartedAt = null;
                    membership.GracePeriodMonthsUsed = 0;
                }
            }
        }

        public async Task<ServiceResult<int>> AssignRoleBasedContributionsAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var currentMonth = (MonthType)now.Month;
            var currentYear = now.Year;
            var periodStart = new DateTimeOffset(new DateTime(currentYear, now.Month, 1), TimeSpan.Zero);
            var periodEnd = periodStart.AddMonths(1).AddSeconds(-1);

            var roleToContributionType = new Dictionary<Infrastructure.Models.Identity.RoleType, ContributionTypeDB>
            {
                { Infrastructure.Models.Identity.RoleType.Administrator, ContributionTypeDB.Administrator },
                { Infrastructure.Models.Identity.RoleType.Moderator, ContributionTypeDB.CommunityModeration },
                { Infrastructure.Models.Identity.RoleType.Support, ContributionTypeDB.CommunitySupport },
            };

            var createdCount = 0;

            foreach (var (roleType, contributionType) in roleToContributionType)
            {
                // Get all users with this role
                var usersWithRole = await _identityContext.UserRoles
                    .Where(ur => ur.Role.RoleName == roleType)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                foreach (var userId in usersWithRole)
                {
                    var membership = await _applicationContext.Memberships
                        .FirstOrDefaultAsync(m => m.UserId == userId);

                    if (membership == null)
                        continue;

                    // Check if contribution already exists for this month
                    var existingContribution = await _applicationContext.Contributions
                        .AnyAsync(c => c.MembershipId == membership.Id
                            && c.Month == currentMonth
                            && c.Year == currentYear
                            && c.Type == contributionType);

                    if (existingContribution)
                        continue;

                    // Create auto-verified contribution
                    var contribution = new ContributionDB
                    {
                        Id = Guid.NewGuid(),
                        Type = contributionType,
                        Status = ContributionStatusDB.AutoVerified,
                        SubmittedAt = now,
                        ContributionPeriodStart = periodStart,
                        ContributionPeriodEnd = periodEnd,
                        Month = currentMonth,
                        Year = currentYear,
                        MembershipId = membership.Id,
                        SubmittedByMembershipId = membership.Id,
                        ValidatedAt = now,
                        Description = $"Auto-assigned for {roleType} role"
                    };

                    _applicationContext.Contributions.Add(contribution);
                    createdCount++;
                }
            }

            await _applicationContext.SaveChangesAsync();

            // Update membership status for affected users
            var affectedMembershipIds = await _applicationContext.Contributions
                .Where(c => c.Month == currentMonth && c.Year == currentYear && c.Type.IsRoleBased())
                .Select(c => c.MembershipId)
                .Distinct()
                .ToListAsync();

            foreach (var membershipId in affectedMembershipIds)
            {
                var membership = await _applicationContext.Memberships
                    .Include(m => m.Contributions)
                    .FirstOrDefaultAsync(m => m.Id == membershipId);

                if (membership != null)
                {
                    await UpdateMembershipStatusAsync(membership);
                }
            }

            await _applicationContext.SaveChangesAsync();

            return ServiceResult<int>.Ok(createdCount);
        }

        private async Task<ContributionDTO> MapToContributionDTOAsync(ContributionDB contribution)
        {
            var membership = await _applicationContext.Memberships.FindAsync(contribution.MembershipId);
            var submitterMembership = await _applicationContext.Memberships.FindAsync(contribution.SubmittedByMembershipId);
            var validatorMembership = contribution.ValidatedByMembershipId.HasValue
                ? await _applicationContext.Memberships.FindAsync(contribution.ValidatedByMembershipId.Value)
                : null;

            var submittedBy = submitterMembership != null
                ? await _identityContext.Users.FindAsync(submitterMembership.UserId)
                : null;
            var validatedBy = validatorMembership != null
                ? await _identityContext.Users.FindAsync(validatorMembership.UserId)
                : null;

            return new ContributionDTO
            {
                Id = contribution.Id,
                Type = (ContributionTypeDTO)(int)contribution.Type,
                Status = (ContributionStatusDTO)(int)contribution.Status,
                SubmittedAt = contribution.SubmittedAt,
                ContributionPeriodStart = contribution.ContributionPeriodStart,
                ContributionPeriodEnd = contribution.ContributionPeriodEnd,
                Month = (int)contribution.Month,
                Year = contribution.Year,
                MembershipId = contribution.MembershipId,
                SubmittedById = submitterMembership?.UserId,
                SubmittedByUsername = submittedBy?.Username,
                ValidatedById = validatorMembership?.UserId,
                ValidatedByUsername = validatedBy?.Username,
                ValidatedAt = contribution.ValidatedAt,
                Description = contribution.Description,
                ExternalReference = contribution.ExternalReference
            };
        }
    }
}
