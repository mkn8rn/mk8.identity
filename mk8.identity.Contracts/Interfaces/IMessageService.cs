using mk8.identity.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace mk8.identity.Contracts.Interfaces
{
    public interface IMessageService
    {
        // support requests - anyone can create
        Task<ServiceResult<MessageDTO>> CreateSupportRequestAsync(Guid userId, SupportRequestCreateDTO request);
        Task<ServiceResult<List<MessageDTO>>> GetSupportRequestsAsync(MessageStatusDTO? status = null);

        // matrix account requests - only active members can create
        Task<ServiceResult<MessageDTO>> CreateMatrixAccountRequestAsync(Guid userId, MatrixAccountRequestCreateDTO request);
        Task<ServiceResult<List<MessageDTO>>> GetMatrixAccountRequestsAsync(MessageStatusDTO? status = null);

        // admin actions
        Task<ServiceResult<MessageDTO>> UpdateMessageStatusAsync(Guid handlerId, MessageUpdateStatusDTO update);
        Task<ServiceResult<MessageDTO>> CompleteMatrixAccountRequestAsync(Guid adminId, MatrixAccountRequestCompleteDTO completion);

        // queries
        Task<ServiceResult<MessageDTO>> GetByIdAsync(Guid messageId);
        Task<ServiceResult<List<MessageDTO>>> GetMessagesForUserAsync(Guid userId);
        Task<ServiceResult<List<MessageDTO>>> GetAllMessagesAsync(MessageTypeDTO? type = null, MessageStatusDTO? status = null);
        Task<ServiceResult<int>> GetPendingMessageCountAsync();
    }
}
