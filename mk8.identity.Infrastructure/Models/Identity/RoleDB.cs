using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Infrastructure.Models.Identity
{
    public enum RoleType
    {
        None = 0,
        Administrator = 1,
        Assessor = 2,

        Moderator = 101,
        Support = 102,
    }

    public class RoleDB
    {
        public Guid Id { get; set; }
        public required RoleType RoleName { get; set; }

        // users with this role (many-to-many via join table)
        public List<UserRoleDB> UserRoles { get; set; } = [];
    }

    // Join table for User-Role many-to-many relationship
    public class UserRoleDB
    {
        public Guid UserId { get; set; }
        public UserDB User { get; set; } = null!;

        public Guid RoleId { get; set; }
        public RoleDB Role { get; set; } = null!;

        public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
