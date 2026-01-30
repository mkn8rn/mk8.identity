using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Contracts.Models
{
    public enum MessageTypeDTO
    {
        Invalid = 0,
        SupportRequest = 1,
        MatrixAccountCreationRequest = 2,
    }

    public enum MessageStatusDTO
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Rejected = 3,
    }

    public class MessageDTO
    {
        public Guid Id { get; set; }
        public required MessageTypeDTO Type { get; set; }
        public required MessageStatusDTO Status { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
        public required string SenderUsername { get; set; }
        public required Guid SenderId { get; set; }

        // support request fields
        public string? Title { get; set; }
        public string? Description { get; set; }

        // matrix request fields
        public string? DesiredMatrixUsername { get; set; }

        // admin response
        public string? HandledByUsername { get; set; }
        public DateTimeOffset? HandledAt { get; set; }
        public string? SpecialInstructions { get; set; }
    }

    public class SupportRequestCreateDTO
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
    }

    public class MatrixAccountRequestCreateDTO
    {
        public required string DesiredUsername { get; set; }
    }

    public class MatrixAccountRequestCompleteDTO
    {
        public required Guid MessageId { get; set; }
        public required string TemporaryPassword { get; set; }
        public string? SpecialInstructions { get; set; }
    }

    public class MessageUpdateStatusDTO
    {
        public required Guid MessageId { get; set; }
        public required MessageStatusDTO NewStatus { get; set; }
    }
}
