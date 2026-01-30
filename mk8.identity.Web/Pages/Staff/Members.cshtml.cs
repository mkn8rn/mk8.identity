using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;

namespace mk8.identity.Web.Pages.Staff
{
    [Authorize(Policy = "Assessor")]
    public class MembersModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IMembershipService _membershipService;

        public MembersModel(IUserService userService, IMembershipService membershipService)
        {
            _userService = userService;
            _membershipService = membershipService;
        }

        public List<UserDTO> Users { get; set; } = [];
        public Dictionary<Guid, MembershipStatusDTO> Memberships { get; set; } = [];
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var usersResult = await _userService.GetAllUsersAsync();
            if (usersResult.Success && usersResult.Data != null)
            {
                Users = usersResult.Data;

                foreach (var user in Users)
                {
                    var membershipResult = await _membershipService.GetMembershipStatusAsync(user.Id);
                    if (membershipResult.Success && membershipResult.Data != null)
                    {
                        Memberships[user.Id] = membershipResult.Data;
                    }
                }
            }

            if (TempData["SuccessMessage"] is string msg)
                SuccessMessage = msg;

            return Page();
        }
    }
}
