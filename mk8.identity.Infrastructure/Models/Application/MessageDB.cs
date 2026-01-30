using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Infrastructure.Models.Application
{
    public enum MessageTypeDB
    {
        Invalid = 0,
        SupportRequest = 1,
        MatrixAccountCreationRequest = 2,
    }

    public enum MessageStatusDB
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Rejected = 3,
    }

    public class MessageDB
    {
        public Guid Id { get; set; }
        public required MessageTypeDB Type { get; set; }
        public required MessageStatusDB Status { get; set; }
        public required DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // who sent the message
        public required Guid SenderMembershipId { get; set; }
        public UserMembershipDB Sender { get; set; } = null!;

        // for support requests
        public string? Title { get; set; }
        public string? Description { get; set; }

        // for matrix account creation requests
        public string? DesiredMatrixUsername { get; set; }

        // admin response fields (for matrix account creation)
        public Guid? HandledByMembershipId { get; set; }
        public UserMembershipDB? HandledBy { get; set; }
        public DateTimeOffset? HandledAt { get; set; }
        public string? TemporaryPassword { get; set; }
        public string? SpecialInstructions { get; set; }

        // the matrix account created (if applicable)
        public Guid? CreatedMatrixAccountId { get; set; }
        public MatrixAccountDB? CreatedMatrixAccount { get; set; }
    }
}
