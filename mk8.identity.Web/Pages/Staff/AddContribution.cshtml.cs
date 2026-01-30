using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace mk8.identity.Web.Pages.Staff
{
    [Authorize(Policy = "Assessor")]
    public class AddContributionModel : PageModel
    {
        private readonly IContributionService _contributionService;
        private readonly IUserService _userService;

        public AddContributionModel(IContributionService contributionService, IUserService userService)
        {
            _contributionService = contributionService;
            _userService = userService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? Username { get; set; }
        public string? ErrorMessage { get; set; }

        public List<SelectListItem> ContributionTypes { get; set; } = [];
        public List<SelectListItem> Months { get; set; } = [];
        public List<SelectListItem> Years { get; set; } = [];

        public class InputModel
        {
            [Required]
            public ContributionTypeDTO Type { get; set; }

            [Required]
            [Range(1, 12)]
            public int Month { get; set; }

            [Required]
            public int Year { get; set; }

            [StringLength(1000)]
            public string? Description { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(Guid userId)
        {
            var userResult = await _userService.GetByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return RedirectToPage("/Staff/Members");

            Username = userResult.Data.Username;
            InitializeSelectLists();
            Input.Month = DateTime.UtcNow.Month;
            Input.Year = DateTime.UtcNow.Year;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid userId)
        {
            var userResult = await _userService.GetByIdAsync(userId);
            if (!userResult.Success || userResult.Data == null)
                return RedirectToPage("/Staff/Members");

            Username = userResult.Data.Username;
            InitializeSelectLists();

            if (!ModelState.IsValid)
                return Page();

            var assessorId = GetUserId();
            if (assessorId == null)
                return RedirectToPage("/Account/Login");

            var result = await _contributionService.CreateAndValidateContributionAsync(assessorId.Value, new ContributionCreateByAssessorDTO
            {
                UserId = userId,
                Type = Input.Type,
                Month = Input.Month,
                Year = Input.Year,
                Description = Input.Description
            });

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to add contribution.";
                return Page();
            }

            TempData["SuccessMessage"] = $"Contribution added for {Username}.";
            return RedirectToPage("/Staff/Members");
        }

        private void InitializeSelectLists()
        {
            ContributionTypes =
            [
                new("Administrator", ((int)ContributionTypeDTO.Administrator).ToString()),
                new("Community Moderation", ((int)ContributionTypeDTO.CommunityModeration).ToString()),
                new("Community Support", ((int)ContributionTypeDTO.CommunitySupport).ToString()),
                new("GitHub Subscription", ((int)ContributionTypeDTO.GithubSubscription).ToString()),
                new("Private Donation", ((int)ContributionTypeDTO.PrivateDonation).ToString()),
                new("Expert Knowledge", ((int)ContributionTypeDTO.ExpertKnowledge).ToString()),
                new("Project Collaboration", ((int)ContributionTypeDTO.ProjectCollaboration).ToString()),
            ];

            Months = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem(
                    System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m),
                    m.ToString()))
                .ToList();

            var currentYear = DateTime.UtcNow.Year;
            Years = Enumerable.Range(currentYear - 2, 4)
                .Select(y => new SelectListItem(y.ToString(), y.ToString()))
                .ToList();
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
