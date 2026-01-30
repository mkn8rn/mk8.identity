using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Contracts.Models
{
    public class UserMembershipDTO
    {
        public Guid Id { get; set; }
        public required bool IsActive { get; set; }
        public required DateTimeOffset StartDate { get; set; }
        public bool IsInGracePeriod { get; set; }
        public DateTimeOffset? GracePeriodStartedAt { get; set; }
        public int GracePeriodMonthsEarned { get; set; }
        public int GracePeriodMonthsUsed { get; set; }
        public int GracePeriodMonthsRemaining => GracePeriodMonthsEarned - GracePeriodMonthsUsed;
        public DateTimeOffset? ExpiresAt { get; set; }
        public List<ContributionDTO> Contributions { get; set; } = [];
    }

    public class MembershipStatusDTO
    {
        public required Guid UserId { get; set; }
        public required string Username { get; set; }
        public required bool IsActive { get; set; }
        public required bool IsInGracePeriod { get; set; }
        public required DateTimeOffset StartDate { get; set; }
        public int GracePeriodMonthsEarned { get; set; }
        public int GracePeriodMonthsRemaining { get; set; }
        public DateTimeOffset? LastContributionDate { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public int TotalYearsAsMember { get; set; }
    }

    public class MembershipCheckResultDTO
    {
        public required Guid UserId { get; set; }
        public required string Username { get; set; }
        public required bool WasActive { get; set; }
        public required bool IsNowActive { get; set; }
        public required bool EnteredGracePeriod { get; set; }
        public required bool WasDeactivated { get; set; }
        public int? GracePeriodMonth { get; set; }
        public int? GracePeriodMonthsRemaining { get; set; }
    }
}
