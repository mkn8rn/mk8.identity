using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Contracts.Models
{
    public class MatrixAccountDTO
    {
        public Guid Id { get; set; }
        public required string AccountId { get; set; }
        public required string Username { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
        public required bool IsDisabled { get; set; }
        public DateTimeOffset? DisabledAt { get; set; }
        public required string OwnerUsername { get; set; }
        public required Guid OwnerId { get; set; }
    }

    public class MatrixAccountCreateDTO
    {
        public required Guid OwnerId { get; set; }
        public required string Username { get; set; }
        public required string TemporaryPassword { get; set; }
        public string? SpecialInstructions { get; set; }
    }

    public class MatrixAccountDisableDTO
    {
        public required Guid MatrixAccountId { get; set; }
    }
}
