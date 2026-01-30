using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System.Security.Claims;

namespace mk8.identity.Web.Pages.Staff
{
    [Authorize(Policy = "Admin")]
    public class MatrixAccountsModel : PageModel
    {
        private readonly IMatrixAccountService _matrixAccountService;

        public MatrixAccountsModel(IMatrixAccountService matrixAccountService)
        {
            _matrixAccountService = matrixAccountService;
        }

        public List<MatrixAccountDTO> Accounts { get; set; } = [];
        public string Filter { get; set; } = "all";
        public string? SuccessMessage { get; set; }
        public int RequiresDisableCount { get; set; }

        public async Task<IActionResult> OnGetAsync(string? filter)
        {
            Filter = filter ?? "all";

            var requiresDisableResult = await _matrixAccountService.GetAccountsRequiringDisableAsync();
            RequiresDisableCount = requiresDisableResult.Success ? requiresDisableResult.Data?.Count ?? 0 : 0;

            if (Filter == "requires-disable")
            {
                if (requiresDisableResult.Success && requiresDisableResult.Data != null)
                    Accounts = requiresDisableResult.Data;
            }
            else
            {
                bool? isDisabled = Filter switch
                {
                    "active" => false,
                    "disabled" => true,
                    _ => null
                };

                var result = await _matrixAccountService.GetAllMatrixAccountsAsync(isDisabled);
                if (result.Success && result.Data != null)
                    Accounts = result.Data;
            }

            if (TempData["SuccessMessage"] is string msg)
                SuccessMessage = msg;

            return Page();
        }

        public async Task<IActionResult> OnPostDisableAsync(Guid accountId)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var result = await _matrixAccountService.DisableMatrixAccountAsync(userId.Value, new MatrixAccountDisableDTO
            {
                MatrixAccountId = accountId
            });

            if (result.Success)
                TempData["SuccessMessage"] = "Matrix account disabled. Remember to disable it on the Matrix server as well.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEnableAsync(Guid accountId)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToPage("/Account/Login");

            var result = await _matrixAccountService.EnableMatrixAccountAsync(userId.Value, accountId);

            if (result.Success)
                TempData["SuccessMessage"] = "Matrix account enabled. Remember to enable it on the Matrix server as well.";
            else
                TempData["SuccessMessage"] = result.ErrorMessage ?? "Failed to enable account.";

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
