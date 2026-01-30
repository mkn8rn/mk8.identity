using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.Security.Claims;

namespace mk8.identity.Web.Pages.Member
{
    [Authorize]
    public class ContributionsModel : PageModel
    {
        private readonly IContributionService _contributionService;

        public ContributionsModel(IContributionService contributionService)
        {
            _contributionService = contributionService;
        }

        public List<ContributionDTO> Contributions { get; set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var result = await _contributionService.GetUserContributionsAsync(userId.Value);
            if (result.Success && result.Data != null)
                Contributions = result.Data;

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
