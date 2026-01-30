using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Contracts.Models
{
    public class RefreshTokenDTO
    {
        public required string Token { get; set; }
        public required DateTimeOffset ExpiresAt { get; set; }
    }
}
