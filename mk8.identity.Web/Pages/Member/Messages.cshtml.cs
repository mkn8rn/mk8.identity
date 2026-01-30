using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.Security.Claims;

namespace mk8.identity.Web.Pages.Member
{
    [Authorize]
    public class MessagesModel : PageModel
    {
        private readonly IMessageService _messageService;
        private readonly IMembershipService _membershipService;
        private readonly IMatrixAccountService _matrixAccountService;

        public MessagesModel(
            IMessageService messageService,
            IMembershipService membershipService,
            IMatrixAccountService matrixAccountService)
        {
            _messageService = messageService;
            _membershipService = membershipService;
            _matrixAccountService = matrixAccountService;
        }

        public List<MessageDTO> Messages { get; set; } = [];
        public bool CanRequestMatrix { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var messagesResult = await _messageService.GetMessagesForUserAsync(userId.Value);
            if (messagesResult.Success && messagesResult.Data != null)
                Messages = messagesResult.Data;

            var membershipResult = await _membershipService.GetMembershipStatusAsync(userId.Value);
            var matrixResult = await _matrixAccountService.UserHasActiveMatrixAccountAsync(userId.Value);

            CanRequestMatrix = membershipResult.Success 
                && membershipResult.Data?.IsActive == true 
                && (!matrixResult.Success || !matrixResult.Data);

            return Page();
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
