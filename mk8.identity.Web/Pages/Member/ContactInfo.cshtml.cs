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
    public class ContactInfoModel : PageModel
    {
        private readonly IContactInfoService _contactInfoService;

        public ContactInfoModel(IContactInfoService contactInfoService)
        {
            _contactInfoService = contactInfoService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [EmailAddress]
            [StringLength(255)]
            public string? Email { get; set; }

            [StringLength(255)]
            public string? Matrix { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var result = await _contactInfoService.GetContactInfoAsync(userId.Value);
            if (result.Success && result.Data != null)
            {
                Input.Email = result.Data.Email;
                Input.Matrix = result.Data.Matrix;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var result = await _contactInfoService.UpdateContactInfoAsync(userId.Value, new ContactInfoUpdateDTO
            {
                Email = Input.Email,
                Matrix = Input.Matrix
            });

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to update contact information.";
                return Page();
            }

            SuccessMessage = "Contact information updated successfully!";
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
