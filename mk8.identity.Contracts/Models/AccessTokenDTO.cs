using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Contracts.Models
{
    public class AccessTokenDTO
    {
        public required string Token { get; set; }
        public required DateTimeOffset ExpiresAt { get; set; }
    }

    public class AuthTokensDTO
    {
        public required AccessTokenDTO AccessToken { get; set; }
        public required RefreshTokenDTO RefreshToken { get; set; }
    }
}
