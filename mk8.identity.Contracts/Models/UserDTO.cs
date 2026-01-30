using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Contracts.Models
{
    public class UserDTO
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
        public List<RoleDTO>? Roles { get; set; }
        public UserMembershipDTO? MembershipStatus { get; set; }
        public PrivilegesDTO? Privileges { get; set; }
    }

    public class UserRegistrationDTO
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class UserLoginDTO
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class UserProfileDTO
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
        public bool IsActiveMember { get; set; }
        public bool IsInGracePeriod { get; set; }
        public int? GracePeriodMonthsRemaining { get; set; }
        public DateTimeOffset? MembershipExpiresAt { get; set; }
        public List<string> RoleNames { get; set; } = [];
    }
}
