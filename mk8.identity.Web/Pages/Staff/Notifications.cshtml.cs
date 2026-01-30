using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.Security.Claims;

namespace mk8.identity.Web.Pages.Staff
{
    [Authorize(Policy = "Staff")]
    public class NotificationsModel : PageModel
    {
        private readonly INotificationService _notificationService;

        public NotificationsModel(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public List<NotificationDTO> Notifications { get; set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            var role = GetHighestRole();
            var result = await _notificationService.GetNotificationsForRoleAsync(role);
            if (result.Success && result.Data != null)
                Notifications = result.Data;

            return Page();
        }

        public async Task<IActionResult> OnPostMarkReadAsync(Guid notificationId)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            await _notificationService.MarkAsReadAsync(notificationId, userId.Value);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMarkCompleteAsync(Guid notificationId)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            await _notificationService.MarkActionCompletedAsync(notificationId, userId.Value);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMarkAllReadAsync()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var role = GetHighestRole();
            await _notificationService.MarkAllAsReadForRoleAsync(role, userId.Value);
            return RedirectToPage();
        }

        private RoleTypeDTO GetHighestRole()
        {
            if (User.IsInRole("Administrator"))
                return RoleTypeDTO.Administrator;
            if (User.IsInRole("Assessor"))
                return RoleTypeDTO.Assessor;
            if (User.IsInRole("Moderator"))
                return RoleTypeDTO.Moderator;
            if (User.IsInRole("Support"))
                return RoleTypeDTO.Support;
            return RoleTypeDTO.None;
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
