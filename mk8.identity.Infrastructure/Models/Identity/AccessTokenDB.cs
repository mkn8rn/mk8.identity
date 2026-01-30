using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Infrastructure.Models.Identity
{
    public class AccessTokenDB
    {
        public Guid Id { get; set; }
        public required string Token { get; set; }
        public required DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
        public required DateTimeOffset ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }

        // user relationship
        public required Guid UserId { get; set; }
        public UserDB User { get; set; } = null!;

        // linked refresh token
        public Guid? RefreshTokenId { get; set; }
        public RefreshTokenDB? RefreshToken { get; set; }
    }
}
