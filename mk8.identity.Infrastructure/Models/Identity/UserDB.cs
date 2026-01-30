using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Infrastructure.Models.Identity
{
    public class UserDB
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public required string PasswordSalt { get; set; }
        public required DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // roles (many-to-many via join table)
        public List<UserRoleDB> UserRoles { get; set; } = [];

        // logins 
        public List<AccessTokenDB> AccessTokens { get; set; } = [];
        public List<RefreshTokenDB> RefreshTokens { get; set; } = [];
    }
}
