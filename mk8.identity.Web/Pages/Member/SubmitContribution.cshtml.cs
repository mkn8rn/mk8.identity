using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace mk8.identity.Web.Pages.Member
{
    [Authorize]
    public class SubmitContributionModel : PageModel
    {
        private readonly IContributionService _contributionService;

        public SubmitContributionModel(IContributionService contributionService)
        {
            _contributionService = contributionService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

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

        public void OnGet()
        {
            InitializeSelectLists();
            Input.Month = DateTime.UtcNow.Month;
            Input.Year = DateTime.UtcNow.Year;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            InitializeSelectLists();

            if (!ModelState.IsValid)
                return Page();

            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var result = await _contributionService.SubmitContributionAsync(userId.Value, new ContributionSubmitDTO
            {
                Type = Input.Type,
                Month = Input.Month,
                Year = Input.Year,
                Description = Input.Description
            });

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to submit contribution.";
                return Page();
            }

            SuccessMessage = "Contribution submitted successfully! An assessor will review it shortly.";
            Input = new InputModel { Month = DateTime.UtcNow.Month, Year = DateTime.UtcNow.Year };
            return Page();
        }

        private void InitializeSelectLists()
        {
            // Only show member-submittable contribution types
            ContributionTypes =
            [
                new("Expert Knowledge", ((int)ContributionTypeDTO.ExpertKnowledge).ToString()),
                new("Project Collaboration", ((int)ContributionTypeDTO.ProjectCollaboration).ToString()),
            ];

            Months = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem(
                    System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m),
                    m.ToString()))
                .ToList();

            var currentYear = DateTime.UtcNow.Year;
            Years = Enumerable.Range(currentYear - 1, 3)
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
