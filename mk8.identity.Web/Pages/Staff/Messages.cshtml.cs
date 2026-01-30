using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.Security.Claims;

namespace mk8.identity.Web.Pages.Staff
{
    [Authorize(Policy = "Staff")]
    public class MessagesModel : PageModel
    {
        private readonly IMessageService _messageService;

        public MessagesModel(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public List<MessageDTO> Messages { get; set; } = [];
        public string Filter { get; set; } = "all";
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string? filter)
        {
            Filter = filter ?? "all";

            MessageTypeDTO? typeFilter = Filter switch
            {
                "support" => MessageTypeDTO.SupportRequest,
                "matrix" => MessageTypeDTO.MatrixAccountCreationRequest,
                _ => null
            };

            MessageStatusDTO? statusFilter = Filter == "pending" ? MessageStatusDTO.Pending : null;

            var result = await _messageService.GetAllMessagesAsync(typeFilter, statusFilter);
            if (result.Success && result.Data != null)
                Messages = result.Data;

            if (TempData["SuccessMessage"] is string msg)
                SuccessMessage = msg;

            return Page();
        }

        public async Task<IActionResult> OnPostInProgressAsync(Guid messageId)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            await _messageService.UpdateMessageStatusAsync(userId.Value, new MessageUpdateStatusDTO
            {
                MessageId = messageId,
                NewStatus = MessageStatusDTO.InProgress
            });

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompleteAsync(Guid messageId)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            await _messageService.UpdateMessageStatusAsync(userId.Value, new MessageUpdateStatusDTO
            {
                MessageId = messageId,
                NewStatus = MessageStatusDTO.Completed
            });

            TempData["SuccessMessage"] = "Message marked as completed.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(Guid messageId)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            await _messageService.UpdateMessageStatusAsync(userId.Value, new MessageUpdateStatusDTO
            {
                MessageId = messageId,
                NewStatus = MessageStatusDTO.Rejected
            });

            TempData["SuccessMessage"] = "Message rejected.";
            return RedirectToPage();
        }

        private Guid? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && Guid.TryParse(claim.Value, out var userId))
                return userId;
            return null;
        }
    }
}
