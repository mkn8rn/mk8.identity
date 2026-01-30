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
    public class NotificationService : INotificationService
    {
        private readonly ApplicationContext _applicationContext;
        private readonly IdentityContext _identityContext;

        public NotificationService(ApplicationContext applicationContext, IdentityContext identityContext)
        {
            _applicationContext = applicationContext;
            _identityContext = identityContext;
        }

        public async Task<ServiceResult<NotificationDTO>> CreateNotificationAsync(NotificationCreateDTO notification)
        {
            Guid? relatedMembershipId = null;
            if (notification.RelatedUserId.HasValue)
            {
                var membership = await _applicationContext.Memberships
                    .FirstOrDefaultAsync(m => m.UserId == notification.RelatedUserId.Value);
                relatedMembershipId = membership?.Id;
            }

            var dbNotification = new NotificationDB
            {
                Id = Guid.NewGuid(),
                Type = (NotificationTypeDB)(int)notification.Type,
                Priority = (NotificationPriorityDB)(int)notification.Priority,
                Title = notification.Title,
                Message = notification.Message,
                CreatedAt = DateTimeOffset.UtcNow,
                IsRead = false,
                IsActionRequired = notification.IsActionRequired,
                IsActionCompleted = false,
                RelatedMembershipId = relatedMembershipId,
                MinimumRoleRequired = (RoleTypeDB)(int)notification.MinimumRoleRequired,
                GracePeriodMonth = notification.GracePeriodMonth,
                GracePeriodMonthsRemaining = notification.GracePeriodMonthsRemaining
            };

            _applicationContext.Notifications.Add(dbNotification);
            await _applicationContext.SaveChangesAsync();

            return ServiceResult<NotificationDTO>.Ok(await MapToNotificationDTOAsync(dbNotification));
        }

        public async Task<ServiceResult> CreateNewUserNotificationAsync(Guid userId, string username)
        {
            var notification = new NotificationCreateDTO
            {
                Type = NotificationTypeDTO.NewUserRegistered,
                Priority = NotificationPriorityDTO.Normal,
                Title = "New User Registered",
                Message = $"A new user '{username}' has registered an account.",
                RelatedUserId = userId,
                IsActionRequired = false,
                MinimumRoleRequired = RoleTypeDTO.Assessor
            };

            await CreateNotificationAsync(notification);
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> CreateContributionSubmittedNotificationAsync(Guid userId, string username, ContributionTypeDTO contributionType)
        {
            var notification = new NotificationCreateDTO
            {
                Type = NotificationTypeDTO.UserContributionSubmitted,
                Priority = NotificationPriorityDTO.Normal,
                Title = "Contribution Submitted for Review",
                Message = $"User '{username}' has submitted a {contributionType} contribution for validation.",
                RelatedUserId = userId,
                IsActionRequired = true,
                MinimumRoleRequired = RoleTypeDTO.Assessor
            };

            await CreateNotificationAsync(notification);
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> CreateGracePeriodNotificationAsync(Guid userId, string username, int gracePeriodMonth, int monthsRemaining)
        {
            string message;
            NotificationPriorityDTO priority;

            if (gracePeriodMonth == 1)
            {
                message = $"Member '{username}' has entered their grace period. They have {monthsRemaining} months remaining before account disablement.";
                priority = NotificationPriorityDTO.Normal;
            }
            else
            {
                message = $"Member '{username}' has started month {gracePeriodMonth} of their grace period. They have {monthsRemaining} more months left.";
                priority = monthsRemaining <= 2 ? NotificationPriorityDTO.High : NotificationPriorityDTO.Normal;
            }

            var notification = new NotificationCreateDTO
            {
                Type = NotificationTypeDTO.MembershipGracePeriodReminder,
                Priority = priority,
                Title = $"Grace Period Update - {username}",
                Message = message,
                RelatedUserId = userId,
                IsActionRequired = false,
                MinimumRoleRequired = RoleTypeDTO.Assessor,
                GracePeriodMonth = gracePeriodMonth,
                GracePeriodMonthsRemaining = monthsRemaining
            };

            await CreateNotificationAsync(notification);
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> CreateMembershipDeactivatedNotificationAsync(Guid userId, string username)
        {
            var notification = new NotificationCreateDTO
            {
                Type = NotificationTypeDTO.MembershipDeactivated,
                Priority = NotificationPriorityDTO.High,
                Title = "Membership Deactivated",
                Message = $"Member '{username}' has been deactivated due to expired grace period.",
                RelatedUserId = userId,
                IsActionRequired = false,
                MinimumRoleRequired = RoleTypeDTO.Assessor
            };

            await CreateNotificationAsync(notification);
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> CreateMatrixDisableRequiredNotificationAsync(Guid userId, string username, string matrixUsername)
        {
            var notification = new NotificationCreateDTO
            {
                Type = NotificationTypeDTO.MatrixAccountDisableRequired,
                Priority = NotificationPriorityDTO.Urgent,
                Title = "Matrix Account Requires Disablement",
                Message = $"Member '{username}' has been deactivated but has an active Matrix account (@{matrixUsername}). Please disable it.",
                RelatedUserId = userId,
                IsActionRequired = true,
                MinimumRoleRequired = RoleTypeDTO.Administrator
            };

            await CreateNotificationAsync(notification);
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult<List<NotificationDTO>>> GetNotificationsForRoleAsync(RoleTypeDTO role, bool unreadOnly = false)
        {
            var dbRoleType = (RoleTypeDB)(int)role;

            var query = _applicationContext.Notifications
                .Where(n => n.MinimumRoleRequired <= dbRoleType)
                .OrderByDescending(n => n.CreatedAt);

            if (unreadOnly)
                query = (IOrderedQueryable<NotificationDB>)query.Where(n => !n.IsRead);

            var notifications = await query.ToListAsync();
            var result = new List<NotificationDTO>();

            foreach (var notification in notifications)
            {
                result.Add(await MapToNotificationDTOAsync(notification));
            }

            return ServiceResult<List<NotificationDTO>>.Ok(result);
        }

        public async Task<ServiceResult<List<NotificationDTO>>> GetNotificationsForUserAsync(Guid userId, bool unreadOnly = false)
        {
            var membership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == userId);
            if (membership == null)
                return ServiceResult<List<NotificationDTO>>.Ok([]);

            var query = _applicationContext.Notifications
                .Where(n => n.AssignedToMembershipId == membership.Id)
                .OrderByDescending(n => n.CreatedAt);

            if (unreadOnly)
                query = (IOrderedQueryable<NotificationDB>)query.Where(n => !n.IsRead);

            var notifications = await query.ToListAsync();
            var result = new List<NotificationDTO>();

            foreach (var notification in notifications)
            {
                result.Add(await MapToNotificationDTOAsync(notification));
            }

            return ServiceResult<List<NotificationDTO>>.Ok(result);
        }

        public async Task<ServiceResult<NotificationDTO>> GetByIdAsync(Guid notificationId)
        {
            var notification = await _applicationContext.Notifications.FindAsync(notificationId);
            if (notification == null)
                return ServiceResult<NotificationDTO>.Fail("Notification not found");

            return ServiceResult<NotificationDTO>.Ok(await MapToNotificationDTOAsync(notification));
        }

        public async Task<ServiceResult<int>> GetUnreadCountForRoleAsync(RoleTypeDTO role)
        {
            var dbRoleType = (RoleTypeDB)(int)role;

            var count = await _applicationContext.Notifications
                .Where(n => n.MinimumRoleRequired <= dbRoleType && !n.IsRead)
                .CountAsync();

            return ServiceResult<int>.Ok(count);
        }

        public async Task<ServiceResult> MarkAsReadAsync(Guid notificationId, Guid readByUserId)
        {
            var notification = await _applicationContext.Notifications.FindAsync(notificationId);
            if (notification == null)
                return ServiceResult.Fail("Notification not found");

            notification.IsRead = true;
            notification.ReadAt = DateTimeOffset.UtcNow;
            await _applicationContext.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> MarkActionCompletedAsync(Guid notificationId, Guid completedByUserId)
        {
            var notification = await _applicationContext.Notifications.FindAsync(notificationId);
            if (notification == null)
                return ServiceResult.Fail("Notification not found");

            notification.IsActionCompleted = true;
            notification.ActionCompletedAt = DateTimeOffset.UtcNow;
            await _applicationContext.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> MarkAllAsReadForRoleAsync(RoleTypeDTO role, Guid readByUserId)
        {
            var dbRoleType = (RoleTypeDB)(int)role;

            var notifications = await _applicationContext.Notifications
                .Where(n => n.MinimumRoleRequired <= dbRoleType && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTimeOffset.UtcNow;
            }

            await _applicationContext.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult<int>> DeleteOldNotificationsAsync(int olderThanDays)
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-olderThanDays);

            var oldNotifications = await _applicationContext.Notifications
                .Where(n => n.CreatedAt < cutoffDate && n.IsRead && (!n.IsActionRequired || n.IsActionCompleted))
                .ToListAsync();

            _applicationContext.Notifications.RemoveRange(oldNotifications);
            await _applicationContext.SaveChangesAsync();

            return ServiceResult<int>.Ok(oldNotifications.Count);
        }

        private async Task<NotificationDTO> MapToNotificationDTOAsync(NotificationDB notification)
        {
            string? relatedUsername = null;
            Guid? relatedUserId = null;

            if (notification.RelatedMembershipId.HasValue)
            {
                var membership = await _applicationContext.Memberships.FindAsync(notification.RelatedMembershipId.Value);
                if (membership != null)
                {
                    relatedUserId = membership.UserId;
                    var user = await _identityContext.Users.FindAsync(membership.UserId);
                    relatedUsername = user?.Username;
                }
            }

            return new NotificationDTO
            {
                Id = notification.Id,
                Type = (NotificationTypeDTO)(int)notification.Type,
                Priority = (NotificationPriorityDTO)(int)notification.Priority,
                Title = notification.Title,
                Message = notification.Message,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt,
                IsActionRequired = notification.IsActionRequired,
                IsActionCompleted = notification.IsActionCompleted,
                RelatedUserId = relatedUserId,
                RelatedUsername = relatedUsername,
                GracePeriodMonth = notification.GracePeriodMonth,
                GracePeriodMonthsRemaining = notification.GracePeriodMonthsRemaining
            };
        }
    }
}
