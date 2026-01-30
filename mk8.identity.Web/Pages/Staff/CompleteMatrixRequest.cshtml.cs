using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace mk8.identity.Web.Pages.Staff
{
    [Authorize(Policy = "Admin")]
    public class CompleteMatrixRequestModel : PageModel
    {
        private readonly IMessageService _messageService;

        public CompleteMatrixRequestModel(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public MessageDTO? Message { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(100, MinimumLength = 8)]
            [Display(Name = "Temporary Password")]
            public string TemporaryPassword { get; set; } = string.Empty;

            [StringLength(1000)]
            [Display(Name = "Special Instructions")]
            public string? SpecialInstructions { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var result = await _messageService.GetByIdAsync(id);
            if (!result.Success || result.Data == null)
                return Page();

            if (result.Data.Type != MessageTypeDTO.MatrixAccountCreationRequest)
                return RedirectToPage("/Staff/Messages");

            Message = result.Data;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var messageResult = await _messageService.GetByIdAsync(id);
            if (!messageResult.Success || messageResult.Data == null)
            {
                ErrorMessage = "Request not found.";
                return Page();
            }

            Message = messageResult.Data;

            if (!ModelState.IsValid)
                return Page();

            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var result = await _messageService.CompleteMatrixAccountRequestAsync(userId.Value, new MatrixAccountRequestCompleteDTO
            {
                MessageId = id,
                TemporaryPassword = Input.TemporaryPassword,
                SpecialInstructions = Input.SpecialInstructions
            });

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to complete request.";
                return Page();
            }

            TempData["SuccessMessage"] = "Matrix account created successfully!";
            return RedirectToPage("/Staff/Messages");
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
