using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.Security.Claims;

namespace mk8.identity.Web.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly IMembershipService _membershipService;
        private readonly IContributionService _contributionService;
        private readonly IPrivilegesService _privilegesService;
        private readonly IMatrixAccountService _matrixAccountService;

        public DashboardModel(
            IMembershipService membershipService,
            IContributionService contributionService,
            IPrivilegesService privilegesService,
            IMatrixAccountService matrixAccountService)
        {
            _membershipService = membershipService;
            _contributionService = contributionService;
            _privilegesService = privilegesService;
            _matrixAccountService = matrixAccountService;
        }

        public MembershipStatusDTO? Membership { get; set; }
        public PrivilegesDTO? Privileges { get; set; }
        public List<ContributionDTO> RecentContributions { get; set; } = [];
        public bool HasMatrixAccount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var membershipResult = await _membershipService.GetMembershipStatusAsync(userId.Value);
            if (membershipResult.Success)
                Membership = membershipResult.Data;

            var privilegesResult = await _privilegesService.GetPrivilegesForUserAsync(userId.Value);
            if (privilegesResult.Success)
                Privileges = privilegesResult.Data;

            var contributionsResult = await _contributionService.GetUserContributionsAsync(userId.Value);
            if (contributionsResult.Success && contributionsResult.Data != null)
                RecentContributions = contributionsResult.Data.Take(5).ToList();

            var matrixResult = await _matrixAccountService.UserHasActiveMatrixAccountAsync(userId.Value);
            HasMatrixAccount = matrixResult.Success && matrixResult.Data;

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
