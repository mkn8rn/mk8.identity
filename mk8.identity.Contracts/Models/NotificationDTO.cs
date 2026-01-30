using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Contracts.Models
{
    public enum NotificationTypeDTO
    {
        Invalid = 0,

        // user events
        NewUserRegistered = 1,
        UserContributionSubmitted = 2,

        // membership events
        MembershipActivated = 101,
        MembershipGracePeriodStarted = 102,
        MembershipGracePeriodReminder = 103,
        MembershipDeactivated = 104,

        // matrix account events
        MatrixAccountCreationRequested = 201,
        MatrixAccountDisableRequired = 202,
    }

    public enum NotificationPriorityDTO
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3,
    }

    public class NotificationDTO
    {
        public Guid Id { get; set; }
        public required NotificationTypeDTO Type { get; set; }
        public required NotificationPriorityDTO Priority { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset? ReadAt { get; set; }
        public bool IsActionRequired { get; set; }
        public bool IsActionCompleted { get; set; }
        public string? RelatedUsername { get; set; }
        public Guid? RelatedUserId { get; set; }
        public int? GracePeriodMonth { get; set; }
        public int? GracePeriodMonthsRemaining { get; set; }
    }

    public class NotificationCreateDTO
    {
        public required NotificationTypeDTO Type { get; set; }
        public required NotificationPriorityDTO Priority { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public Guid? RelatedUserId { get; set; }
        public bool IsActionRequired { get; set; }
        public RoleTypeDTO MinimumRoleRequired { get; set; }
        public int? GracePeriodMonth { get; set; }
        public int? GracePeriodMonthsRemaining { get; set; }
    }
}
