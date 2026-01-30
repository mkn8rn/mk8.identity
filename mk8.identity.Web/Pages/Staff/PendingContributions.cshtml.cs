using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.Security.Claims;

namespace mk8.identity.Web.Pages.Staff
{
    [Authorize(Policy = "Assessor")]
    public class PendingContributionsModel : PageModel
    {
        private readonly IContributionService _contributionService;

        public PendingContributionsModel(IContributionService contributionService)
        {
            _contributionService = contributionService;
        }

        public List<ContributionDTO> Contributions { get; set; } = [];
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var result = await _contributionService.GetPendingContributionsAsync();
            if (result.Success && result.Data != null)
                Contributions = result.Data;

            if (TempData["SuccessMessage"] is string msg)
                SuccessMessage = msg;

            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(Guid contributionId)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var result = await _contributionService.ValidateContributionAsync(userId.Value, new ContributionValidateDTO
            {
                ContributionId = contributionId,
                Approved = true
            });

            if (result.Success)
                TempData["SuccessMessage"] = "Contribution approved successfully.";
            else
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to approve contribution.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(Guid contributionId)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var result = await _contributionService.ValidateContributionAsync(userId.Value, new ContributionValidateDTO
            {
                ContributionId = contributionId,
                Approved = false
            });

            if (result.Success)
                TempData["SuccessMessage"] = "Contribution rejected.";
            else
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to reject contribution.";

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
