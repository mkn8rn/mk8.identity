using mk8.identity.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace mk8.identity.Contracts.Interfaces
{
    public interface INotificationService
    {
        // create notifications
        Task<ServiceResult<NotificationDTO>> CreateNotificationAsync(NotificationCreateDTO notification);
        Task<ServiceResult> CreateNewUserNotificationAsync(Guid userId, string username);
        Task<ServiceResult> CreateContributionSubmittedNotificationAsync(Guid userId, string username, ContributionTypeDTO contributionType);
        Task<ServiceResult> CreateGracePeriodNotificationAsync(Guid userId, string username, int gracePeriodMonth, int monthsRemaining);
        Task<ServiceResult> CreateMembershipDeactivatedNotificationAsync(Guid userId, string username);
        Task<ServiceResult> CreateMatrixDisableRequiredNotificationAsync(Guid userId, string username, string matrixUsername);

        // read notifications
        Task<ServiceResult<List<NotificationDTO>>> GetNotificationsForRoleAsync(RoleTypeDTO role, bool unreadOnly = false);
        Task<ServiceResult<List<NotificationDTO>>> GetNotificationsForUserAsync(Guid userId, bool unreadOnly = false);
        Task<ServiceResult<NotificationDTO>> GetByIdAsync(Guid notificationId);
        Task<ServiceResult<int>> GetUnreadCountForRoleAsync(RoleTypeDTO role);

        // update notifications
        Task<ServiceResult> MarkAsReadAsync(Guid notificationId, Guid readByUserId);
        Task<ServiceResult> MarkActionCompletedAsync(Guid notificationId, Guid completedByUserId);
        Task<ServiceResult> MarkAllAsReadForRoleAsync(RoleTypeDTO role, Guid readByUserId);

        // cleanup
        Task<ServiceResult<int>> DeleteOldNotificationsAsync(int olderThanDays);
    }
}
