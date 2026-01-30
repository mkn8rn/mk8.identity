using Microsoft.EntityFrameworkCore;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using mk8.identity.Infrastructure.Contexts;
using mk8.identity.Infrastructure.Models.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mk8.identity.Application.Services
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationContext _applicationContext;
        private readonly IdentityContext _identityContext;
        private readonly INotificationService _notificationService;
        private readonly IMatrixAccountService _matrixAccountService;

        public MessageService(
            ApplicationContext applicationContext,
            IdentityContext identityContext,
            INotificationService notificationService,
            IMatrixAccountService matrixAccountService)
        {
            _applicationContext = applicationContext;
            _identityContext = identityContext;
            _notificationService = notificationService;
            _matrixAccountService = matrixAccountService;
        }

        public async Task<ServiceResult<MessageDTO>> CreateSupportRequestAsync(Guid userId, SupportRequestCreateDTO request)
        {
            var user = await _identityContext.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult<MessageDTO>.Fail("User not found");

            var membership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == userId);
            if (membership == null)
                return ServiceResult<MessageDTO>.Fail("User membership not found");

            var message = new MessageDB
            {
                Id = Guid.NewGuid(),
                Type = MessageTypeDB.SupportRequest,
                Status = MessageStatusDB.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                SenderMembershipId = membership.Id,
                Title = request.Title,
                Description = request.Description
            };

            _applicationContext.Messages.Add(message);
            await _applicationContext.SaveChangesAsync();

            return ServiceResult<MessageDTO>.Ok(MapToMessageDTO(message, user.Username));
        }

        public async Task<ServiceResult<MessageDTO>> CreateMatrixAccountRequestAsync(Guid userId, MatrixAccountRequestCreateDTO request)
        {
            var user = await _identityContext.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult<MessageDTO>.Fail("User not found");

            var membership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == userId);
            if (membership == null)
                return ServiceResult<MessageDTO>.Fail("User membership not found");

            if (!membership.IsActive)
                return ServiceResult<MessageDTO>.Fail("Only active members can request Matrix accounts");

            if (!IsValidMatrixUsername(request.DesiredUsername))
                return ServiceResult<MessageDTO>.Fail("Invalid Matrix username format");

            var existingAccount = await _applicationContext.MatrixAccounts
                .FirstOrDefaultAsync(m => m.Username == request.DesiredUsername);
            if (existingAccount != null)
                return ServiceResult<MessageDTO>.Fail("Matrix username already taken");

            var message = new MessageDB
            {
                Id = Guid.NewGuid(),
                Type = MessageTypeDB.MatrixAccountCreationRequest,
                Status = MessageStatusDB.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                SenderMembershipId = membership.Id,
                DesiredMatrixUsername = request.DesiredUsername
            };

            _applicationContext.Messages.Add(message);
            await _applicationContext.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(new NotificationCreateDTO
            {
                Type = NotificationTypeDTO.MatrixAccountCreationRequested,
                Priority = NotificationPriorityDTO.Normal,
                Title = "Matrix Account Request",
                Message = $"User '{user.Username}' has requested a Matrix account with username: @{request.DesiredUsername}",
                RelatedUserId = userId,
                IsActionRequired = true,
                MinimumRoleRequired = RoleTypeDTO.Administrator
            });

            return ServiceResult<MessageDTO>.Ok(MapToMessageDTO(message, user.Username));
        }

        public async Task<ServiceResult<List<MessageDTO>>> GetSupportRequestsAsync(MessageStatusDTO? status = null)
        {
            var query = _applicationContext.Messages
                .Include(m => m.Sender)
                .Where(m => m.Type == MessageTypeDB.SupportRequest);

            if (status.HasValue)
                query = query.Where(m => m.Status == (MessageStatusDB)(int)status.Value);

            var messages = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();

            var result = new List<MessageDTO>();
            foreach (var message in messages)
            {
                var user = await _identityContext.Users.FindAsync(message.Sender.UserId);
                result.Add(MapToMessageDTO(message, user?.Username ?? "Unknown"));
            }

            return ServiceResult<List<MessageDTO>>.Ok(result);
        }

        public async Task<ServiceResult<List<MessageDTO>>> GetMatrixAccountRequestsAsync(MessageStatusDTO? status = null)
        {
            var query = _applicationContext.Messages
                .Include(m => m.Sender)
                .Where(m => m.Type == MessageTypeDB.MatrixAccountCreationRequest);

            if (status.HasValue)
                query = query.Where(m => m.Status == (MessageStatusDB)(int)status.Value);

            var messages = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();

            var result = new List<MessageDTO>();
            foreach (var message in messages)
            {
                var user = await _identityContext.Users.FindAsync(message.Sender.UserId);
                result.Add(MapToMessageDTO(message, user?.Username ?? "Unknown"));
            }

            return ServiceResult<List<MessageDTO>>.Ok(result);
        }

        public async Task<ServiceResult<MessageDTO>> UpdateMessageStatusAsync(Guid handlerId, MessageUpdateStatusDTO update)
        {
            var message = await _applicationContext.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == update.MessageId);

            if (message == null)
                return ServiceResult<MessageDTO>.Fail("Message not found");

            var handlerMembership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == handlerId);
            if (handlerMembership == null)
                return ServiceResult<MessageDTO>.Fail("Handler membership not found");

            message.Status = (MessageStatusDB)(int)update.NewStatus;
            message.HandledByMembershipId = handlerMembership.Id;
            message.HandledAt = DateTimeOffset.UtcNow;

            await _applicationContext.SaveChangesAsync();

            var senderUser = await _identityContext.Users.FindAsync(message.Sender.UserId);
            return ServiceResult<MessageDTO>.Ok(MapToMessageDTO(message, senderUser?.Username ?? "Unknown"));
        }

        public async Task<ServiceResult<MessageDTO>> CompleteMatrixAccountRequestAsync(Guid adminId, MatrixAccountRequestCompleteDTO completion)
        {
            var message = await _applicationContext.Messages
                .Include(m => m.Sender)
                    .ThenInclude(s => s.Privileges)
                .FirstOrDefaultAsync(m => m.Id == completion.MessageId);

            if (message == null)
                return ServiceResult<MessageDTO>.Fail("Message not found");

            if (message.Type != MessageTypeDB.MatrixAccountCreationRequest)
                return ServiceResult<MessageDTO>.Fail("Message is not a Matrix account request");

            var adminMembership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == adminId);
            if (adminMembership == null)
                return ServiceResult<MessageDTO>.Fail("Admin membership not found");

            var senderUserId = message.Sender.UserId;
            var senderUser = await _identityContext.Users.FindAsync(senderUserId);

            var accountResult = await _matrixAccountService.CreateMatrixAccountAsync(adminId, new MatrixAccountCreateDTO
            {
                OwnerId = senderUserId,
                Username = message.DesiredMatrixUsername!,
                TemporaryPassword = completion.TemporaryPassword
            });

            if (!accountResult.Success)
                return ServiceResult<MessageDTO>.Fail(accountResult.ErrorMessage!);

            message.Status = MessageStatusDB.Completed;
            message.HandledByMembershipId = adminMembership.Id;
            message.HandledAt = DateTimeOffset.UtcNow;
            message.TemporaryPassword = completion.TemporaryPassword;
            message.SpecialInstructions = completion.SpecialInstructions;
            message.CreatedMatrixAccountId = accountResult.Data!.Id;

            await _applicationContext.SaveChangesAsync();



            return ServiceResult<MessageDTO>.Ok(MapToMessageDTO(message, senderUser?.Username ?? "Unknown"));
        }

        public async Task<ServiceResult<MessageDTO>> GetByIdAsync(Guid messageId)
        {
            var message = await _applicationContext.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
                return ServiceResult<MessageDTO>.Fail("Message not found");

            var senderUser = await _identityContext.Users.FindAsync(message.Sender.UserId);
            return ServiceResult<MessageDTO>.Ok(MapToMessageDTO(message, senderUser?.Username ?? "Unknown"));
        }

        public async Task<ServiceResult<List<MessageDTO>>> GetMessagesForUserAsync(Guid userId)
        {
            var membership = await _applicationContext.Memberships.FirstOrDefaultAsync(m => m.UserId == userId);
            if (membership == null)
                return ServiceResult<List<MessageDTO>>.Fail("User membership not found");

            var messages = await _applicationContext.Messages
                .Where(m => m.SenderMembershipId == membership.Id)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var user = await _identityContext.Users.FindAsync(userId);
            var result = messages.Select(m => MapToMessageDTO(m, user?.Username ?? "Unknown")).ToList();
            return ServiceResult<List<MessageDTO>>.Ok(result);
        }

        public async Task<ServiceResult<List<MessageDTO>>> GetAllMessagesAsync(MessageTypeDTO? type = null, MessageStatusDTO? status = null)
        {
            var query = _applicationContext.Messages
                .Include(m => m.Sender)
                .AsQueryable();

            if (type.HasValue)
                query = query.Where(m => m.Type == (MessageTypeDB)(int)type.Value);
            if (status.HasValue)
                query = query.Where(m => m.Status == (MessageStatusDB)(int)status.Value);

            var messages = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();

            var result = new List<MessageDTO>();
            foreach (var message in messages)
            {
                var senderUser = await _identityContext.Users.FindAsync(message.Sender.UserId);
                result.Add(MapToMessageDTO(message, senderUser?.Username ?? "Unknown"));
            }

            return ServiceResult<List<MessageDTO>>.Ok(result);
        }

        public async Task<ServiceResult<int>> GetPendingMessageCountAsync()
        {
            var count = await _applicationContext.Messages
                .Where(m => m.Status == MessageStatusDB.Pending)
                .CountAsync();

            return ServiceResult<int>.Ok(count);
        }

        private static MessageDTO MapToMessageDTO(MessageDB message, string senderUsername)
        {
            return new MessageDTO
            {
                Id = message.Id,
                Type = (MessageTypeDTO)(int)message.Type,
                Status = (MessageStatusDTO)(int)message.Status,
                CreatedAt = message.CreatedAt,
                SenderId = message.Sender?.UserId ?? Guid.Empty,
                SenderUsername = senderUsername,
                Title = message.Title,
                Description = message.Description,
                DesiredMatrixUsername = message.DesiredMatrixUsername,
                HandledAt = message.HandledAt,
                SpecialInstructions = message.SpecialInstructions
            };
        }

        private static bool IsValidMatrixUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 32)
                return false;

            foreach (char c in username)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                    return false;
            }

            return true;
        }
    }
}
