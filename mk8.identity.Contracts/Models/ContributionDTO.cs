using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Contracts.Models
{
    public enum ContributionTypeDTO
    {
        Invalid = 0,

        // Role-based (auto-assigned monthly if user has the role)
        Administrator = 1,
        CommunityModeration = 2,
        CommunitySupport = 3,

        // Payment (auto-verified via external API or assessor-only)
        GithubSubscription = 101,
        PrivateDonation = 102,  // Assessor-only

        // Member-submittable (requires validation)
        ExpertKnowledge = 201,
        ProjectCollaboration = 202,
    }

    public enum ContributionStatusDTO
    {
        Pending = 0,
        Validated = 1,
        Rejected = 2,
        AutoVerified = 3,
    }

    public static class ContributionTypeDTOExtensions
    {
        private static readonly HashSet<ContributionTypeDTO> MemberSubmittableTypes =
        [
            ContributionTypeDTO.ExpertKnowledge,
            ContributionTypeDTO.ProjectCollaboration,
        ];

        private static readonly HashSet<ContributionTypeDTO> RoleBasedTypes =
        [
            ContributionTypeDTO.Administrator,
            ContributionTypeDTO.CommunityModeration,
            ContributionTypeDTO.CommunitySupport,
        ];

        private static readonly HashSet<ContributionTypeDTO> ExternalApiTypes =
        [
            ContributionTypeDTO.GithubSubscription,
        ];

        private static readonly HashSet<ContributionTypeDTO> AssessorOnlyTypes =
        [
            ContributionTypeDTO.PrivateDonation,
        ];

        public static bool IsMemberSubmittable(this ContributionTypeDTO type) 
            => MemberSubmittableTypes.Contains(type);

        public static bool IsRoleBased(this ContributionTypeDTO type) 
            => RoleBasedTypes.Contains(type);

        public static bool IsExternalApi(this ContributionTypeDTO type) 
            => ExternalApiTypes.Contains(type);

        public static bool IsAssessorOnly(this ContributionTypeDTO type) 
            => AssessorOnlyTypes.Contains(type);

        public static bool IsAutoAssigned(this ContributionTypeDTO type) 
            => IsRoleBased(type) || IsExternalApi(type);

        public static IEnumerable<ContributionTypeDTO> GetMemberSubmittableTypes() 
            => MemberSubmittableTypes;
    }

    public class ContributionDTO
    {
        public Guid Id { get; set; }
        public required ContributionTypeDTO Type { get; set; }
        public required ContributionStatusDTO Status { get; set; }
        public required DateTimeOffset SubmittedAt { get; set; }
        public required DateTimeOffset ContributionPeriodStart { get; set; }
        public required DateTimeOffset ContributionPeriodEnd { get; set; }
        public required int Month { get; set; }
        public required int Year { get; set; }
        public Guid? MembershipId { get; set; }
        public Guid? SubmittedById { get; set; }
        public string? SubmittedByUsername { get; set; }
        public Guid? ValidatedById { get; set; }
        public string? ValidatedByUsername { get; set; }
        public DateTimeOffset? ValidatedAt { get; set; }
        public string? Description { get; set; }
        public string? ExternalReference { get; set; }
    }

    public class ContributionSubmitDTO
    {
        public required ContributionTypeDTO Type { get; set; }
        public required int Month { get; set; }
        public required int Year { get; set; }
        public string? Description { get; set; }
    }

    public class ContributionValidateDTO
    {
        public required Guid ContributionId { get; set; }
        public required bool Approved { get; set; }
        public string? Notes { get; set; }
    }

    public class ContributionCreateByAssessorDTO
    {
        public required Guid UserId { get; set; }
        public required ContributionTypeDTO Type { get; set; }
        public required int Month { get; set; }
        public required int Year { get; set; }
        public string? Description { get; set; }
    }
}
