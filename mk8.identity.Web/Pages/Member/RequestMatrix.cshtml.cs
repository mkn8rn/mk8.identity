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
    public class RequestMatrixModel : PageModel
    {
        private readonly IMessageService _messageService;
        private readonly IMembershipService _membershipService;
        private readonly IMatrixAccountService _matrixAccountService;

        public RequestMatrixModel(
            IMessageService messageService,
            IMembershipService membershipService,
            IMatrixAccountService matrixAccountService)
        {
            _messageService = messageService;
            _membershipService = membershipService;
            _matrixAccountService = matrixAccountService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public bool CanRequest { get; set; }
        public string? CannotRequestReason { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(32, MinimumLength = 3)]
            [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Username can only contain letters, numbers, underscores, and hyphens.")]
            [Display(Name = "Desired Username")]
            public string DesiredUsername { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            await CheckCanRequest(userId.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            await CheckCanRequest(userId.Value);

            if (!CanRequest)
                return Page();

            if (!ModelState.IsValid)
                return Page();

            var result = await _messageService.CreateMatrixAccountRequestAsync(userId.Value, new MatrixAccountRequestCreateDTO
            {
                DesiredUsername = Input.DesiredUsername
            });

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to submit request.";
                return Page();
            }

            TempData["SuccessMessage"] = "Matrix account request submitted! An administrator will process it soon.";
            return RedirectToPage("/Member/Messages");
        }

        private async Task CheckCanRequest(Guid userId)
        {
            var membershipResult = await _membershipService.GetMembershipStatusAsync(userId);
            if (!membershipResult.Success || membershipResult.Data?.IsActive != true)
            {
                CanRequest = false;
                CannotRequestReason = "You must have an active membership to request a Matrix account.";
                return;
            }

            var matrixResult = await _matrixAccountService.UserHasActiveMatrixAccountAsync(userId);
            if (matrixResult.Success && matrixResult.Data)
            {
                CanRequest = false;
                CannotRequestReason = "You already have an active Matrix account.";
                return;
            }

            CanRequest = true;
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
