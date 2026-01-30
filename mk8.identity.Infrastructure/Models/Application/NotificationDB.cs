using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Infrastructure.Models.Application
{
    public enum NotificationTypeDB
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

    public enum NotificationPriorityDB
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3,
    }

    public enum RoleTypeDB
    {
        None = 0,
        Administrator = 1,
        Assessor = 2,
        Moderator = 101,
        Support = 102,
    }

    public class NotificationDB
    {
        public Guid Id { get; set; }
        public required NotificationTypeDB Type { get; set; }
        public required NotificationPriorityDB Priority { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public required DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsRead { get; set; }
        public DateTimeOffset? ReadAt { get; set; }
        public bool IsActionRequired { get; set; }
        public bool IsActionCompleted { get; set; }
        public DateTimeOffset? ActionCompletedAt { get; set; }

        // the membership this notification is about (e.g., the member entering grace period)
        public Guid? RelatedMembershipId { get; set; }
        public UserMembershipDB? RelatedMembership { get; set; }

        // who can see this notification (assessor/moderator/admin who reads it)
        // null means visible to all staff with appropriate roles
        public Guid? AssignedToMembershipId { get; set; }
        public UserMembershipDB? AssignedTo { get; set; }

        // minimum role required to view this notification
        public required RoleTypeDB MinimumRoleRequired { get; set; }

        // for grace period reminders - track which month of grace period
        public int? GracePeriodMonth { get; set; }
        public int? GracePeriodMonthsRemaining { get; set; }
    }
}
