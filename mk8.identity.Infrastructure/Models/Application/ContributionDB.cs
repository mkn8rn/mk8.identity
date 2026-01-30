using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Infrastructure.Models.Application
{
    public enum ContributionTypeDB
    {
        Invalid = 0,
        
        // Role-based (auto-assigned monthly if user has the role)
        Administrator = 1,
        CommunityModeration = 2,
        CommunitySupport = 3,

        // Payment (auto-verified via external API or assessor-only)
        GithubSubscription = 101,
        PrivateDonation = 102,  // Assessor-only, manual entry

        // Member-submittable (requires validation)
        ExpertKnowledge = 201,
        ProjectCollaboration = 202,
    }

    public enum ContributionStatusDB
    {
        Pending = 0,
        Validated = 1,
        Rejected = 2,
        AutoVerified = 3,
    }

    public enum MonthType
    {
        January = 1,
        February = 2,
        March = 3,
        April = 4,
        May = 5,
        June = 6,
        July = 7,
        August = 8,
        September = 9,
        October = 10,
        November = 11,
        December = 12
    }

    public static class ContributionTypeExtensions
    {
        /// <summary>
        /// Types that members can manually submit for validation.
        /// </summary>
        private static readonly HashSet<ContributionTypeDB> MemberSubmittableTypes =
        [
            ContributionTypeDB.ExpertKnowledge,
            ContributionTypeDB.ProjectCollaboration,
        ];

        /// <summary>
        /// Types that are auto-assigned based on user roles.
        /// </summary>
        private static readonly HashSet<ContributionTypeDB> RoleBasedTypes =
        [
            ContributionTypeDB.Administrator,
            ContributionTypeDB.CommunityModeration,
            ContributionTypeDB.CommunitySupport,
        ];

        /// <summary>
        /// Types that are auto-verified via external APIs.
        /// </summary>
        private static readonly HashSet<ContributionTypeDB> ExternalApiTypes =
        [
            ContributionTypeDB.GithubSubscription,
        ];

        /// <summary>
        /// Types that can only be added by assessors (manual verification required).
        /// </summary>
        private static readonly HashSet<ContributionTypeDB> AssessorOnlyTypes =
        [
            ContributionTypeDB.PrivateDonation,
        ];

        public static bool IsMemberSubmittable(this ContributionTypeDB type) 
            => MemberSubmittableTypes.Contains(type);

        public static bool IsRoleBased(this ContributionTypeDB type) 
            => RoleBasedTypes.Contains(type);

        public static bool IsExternalApi(this ContributionTypeDB type) 
            => ExternalApiTypes.Contains(type);

        public static bool IsAssessorOnly(this ContributionTypeDB type) 
            => AssessorOnlyTypes.Contains(type);

        public static bool IsAutoAssigned(this ContributionTypeDB type) 
            => IsRoleBased(type) || IsExternalApi(type);
    }

    public class ContributionDB
    {
        public Guid Id { get; set; }
        public required ContributionTypeDB Type { get; set; }
        public required ContributionStatusDB Status { get; set; }
        public required DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
        public required DateTimeOffset ContributionPeriodStart { get; set; }
        public required DateTimeOffset ContributionPeriodEnd { get; set; }

        // not actually used for checks, just for easier querying
        public required MonthType Month { get; set; }
        public required int Year { get; set; }

        // link to membership (the member this contribution is for)
        public required Guid MembershipId { get; set; }
        public UserMembershipDB Membership { get; set; } = null!;

        // who submitted the contribution (member or assessor)
        public required Guid SubmittedByMembershipId { get; set; }
        public UserMembershipDB SubmittedBy { get; set; } = null!;

        // who validated (assessor) - null if auto-verified or pending
        public Guid? ValidatedByMembershipId { get; set; }
        public UserMembershipDB? ValidatedBy { get; set; }
        public DateTimeOffset? ValidatedAt { get; set; }

        // optional description/notes
        public string? Description { get; set; }

        // for auto-verified contributions (e.g., GitHub API reference)
        public string? ExternalReference { get; set; }
    }
}
