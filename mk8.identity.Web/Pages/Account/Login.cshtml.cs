using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace mk8.identity.Web.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IMembershipService _membershipService;

        public LoginModel(IUserService userService, IMembershipService membershipService)
        {
            _userService = userService;
            _membershipService = membershipService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            public string Username { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Dashboard");

            if (!ModelState.IsValid)
                return Page();

            var loginResult = await _userService.LoginAsync(new UserLoginDTO
            {
                Username = Input.Username,
                Password = Input.Password
            });

            if (!loginResult.Success)
            {
                ErrorMessage = loginResult.ErrorMessage ?? "Invalid login attempt.";
                return Page();
            }

            var userResult = await _userService.GetByUsernameAsync(Input.Username);
            if (!userResult.Success || userResult.Data == null)
            {
                ErrorMessage = "Unable to retrieve user information.";
                return Page();
            }

            var user = userResult.Data;
            var rolesResult = await _userService.GetUserRolesAsync(user.Id);
            var membershipResult = await _membershipService.GetMembershipStatusAsync(user.Id);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new("AccessToken", loginResult.Data!.AccessToken.Token),
                new("RefreshToken", loginResult.Data.RefreshToken.Token)
            };

            if (membershipResult.Success && membershipResult.Data != null)
            {
                claims.Add(new Claim("IsActiveMember", membershipResult.Data.IsActive.ToString().ToLower()));
            }

            if (rolesResult.Success && rolesResult.Data != null)
            {
                foreach (var role in rolesResult.Data)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.RoleName.ToString()));
                }
            }

            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);

            return LocalRedirect(returnUrl);
        }
    }
}
