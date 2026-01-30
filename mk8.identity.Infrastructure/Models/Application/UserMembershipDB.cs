using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Infrastructure.Models.Application
{
    public class UserMembershipDB
    {
        public Guid Id { get; set; }
        public required bool IsActive { get; set; }
        public required DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow;
        public List<DateTimeOffset> ActivationDates { get; set; } = [];
        public List<DateTimeOffset> DeactivationDates { get; set; } = [];

        // grace period tracking
        public bool IsInGracePeriod { get; set; }
        public DateTimeOffset? GracePeriodStartedAt { get; set; }
        public int GracePeriodMonthsEarned { get; set; } // +1 per year of membership, max 24
        public int GracePeriodMonthsUsed { get; set; }

        // calculated: when membership actually expires (last contribution end + 1 month + grace)
        public DateTimeOffset? ExpiresAt { get; set; }

        // link to user (foreign key to IdentityContext - no navigation)
        public required Guid UserId { get; set; }

        // contributions for this membership
        public List<ContributionDB> Contributions { get; set; } = [];

        // privileges (one-to-one)
        public PrivilegesDB? Privileges { get; set; }

        // contact info (one-to-one)
        public ContactInfoDB? ContactInfo { get; set; }

        // messages sent by this user
        public List<MessageDB> Messages { get; set; } = [];
    }
}
