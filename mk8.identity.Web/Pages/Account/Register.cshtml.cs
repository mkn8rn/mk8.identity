using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.ComponentModel.DataAnnotations;

namespace mk8.identity.Web.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IUserService _userService;

        public RegisterModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(50, MinimumLength = 3)]
            public string Username { get; set; } = string.Empty;

            [Required]
            [StringLength(100, MinimumLength = 8)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var result = await _userService.RegisterAsync(new UserRegistrationDTO
            {
                Username = Input.Username,
                Password = Input.Password
            });

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Registration failed.";
                return Page();
            }

            TempData["SuccessMessage"] = "Account created successfully! Please login.";
            return RedirectToPage("/Account/Login");
        }
    }
}
