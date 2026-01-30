using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace mk8.identity.Web.Pages.Member
{
    [Authorize]
    public class NewMessageModel : PageModel
    {
        private readonly IMessageService _messageService;

        public NewMessageModel(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(200, MinimumLength = 5)]
            public string Title { get; set; } = string.Empty;

            [Required]
            [StringLength(4000, MinimumLength = 20)]
            public string Description { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var result = await _messageService.CreateSupportRequestAsync(userId.Value, new SupportRequestCreateDTO
            {
                Title = Input.Title,
                Description = Input.Description
            });

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to submit request.";
                return Page();
            }

            TempData["SuccessMessage"] = "Support request submitted successfully!";
            return RedirectToPage("/Member/Messages");
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
