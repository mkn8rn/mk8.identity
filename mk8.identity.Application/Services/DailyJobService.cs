using mk8.identity.Contracts.Interfaces;
using mk8.identity.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace mk8.identity.Application.Services
{
    /// <summary>
    /// Background job service for daily membership checks and cleanup tasks.
    /// Should be registered with a job scheduler (e.g., Hangfire, Quartz.NET) to run daily at 00:00 UTC.
    /// </summary>
    public class DailyJobService
    {
        private readonly IMembershipService _membershipService;
        private readonly IContributionService _contributionService;
        private readonly INotificationService _notificationService;

        public DailyJobService(
            IMembershipService membershipService,
            IContributionService contributionService,
            INotificationService notificationService)
        {
            _membershipService = membershipService;
            _contributionService = contributionService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Main daily job entry point. Runs all daily tasks.
        /// </summary>
        public async Task RunDailyJobsAsync()
        {
            var results = new List<string>();

            // 1. Auto-assign role-based contributions for the current month
            var roleBasedResult = await AssignRoleBasedContributionsAsync();
            results.Add($"Role-based contributions: {roleBasedResult}");

            // 2. Process GitHub subscriptions (auto-verify contributions)
            var githubResult = await ProcessGitHubSubscriptionsAsync();
            results.Add($"GitHub subscriptions processed: {githubResult}");

            // 3. Run membership checks (grace periods, deactivations, notifications)
            var membershipResult = await RunMembershipChecksAsync();
            results.Add($"Membership checks completed: {membershipResult}");

            // 4. Clean up old notifications
            var cleanupResult = await CleanupOldNotificationsAsync();
            results.Add($"Old notifications cleaned up: {cleanupResult}");

            // TODO: Log results
        }

        /// <summary>
        /// Auto-assign contributions for users with staff roles (Administrator, Moderator, Support).
        /// Creates one contribution per role per month, auto-verified.
        /// </summary>
        private async Task<string> AssignRoleBasedContributionsAsync()
        {
            try
            {
                var result = await _contributionService.AssignRoleBasedContributionsAsync();
                if (result.Success)
                {
                    return $"Created {result.Data} contributions";
                }
                return $"Failed: {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Process GitHub sponsorships and create auto-verified contributions.
        /// </summary>
        private async Task<string> ProcessGitHubSubscriptionsAsync()
        {
            try
            {
                var result = await _contributionService.ProcessGitHubSubscriptionsAsync();
                if (result.Success)
                {
                    return $"Created {result.Data?.Count ?? 0} contributions";
                }
                return $"Failed: {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Run membership status checks for all users.
        /// - Activates memberships for users with valid contributions
        /// - Tracks grace period progression
        /// - Deactivates expired memberships
        /// - Creates appropriate notifications
        /// </summary>
        private async Task<string> RunMembershipChecksAsync()
        {
            try
            {
                var result = await _membershipService.RunDailyMembershipCheckAsync();
                if (result.Success && result.Data != null)
                {
                    var activated = result.Data.Count(r => r.IsNowActive && !r.WasActive);
                    var enteredGrace = result.Data.Count(r => r.EnteredGracePeriod);
                    var deactivated = result.Data.Count(r => r.WasDeactivated);

                    return $"Activated: {activated}, Entered grace: {enteredGrace}, Deactivated: {deactivated}";
                }
                return $"Failed: {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Clean up old read notifications (older than 90 days).
        /// </summary>
        private async Task<string> CleanupOldNotificationsAsync()
        {
            try
            {
                var result = await _notificationService.DeleteOldNotificationsAsync(olderThanDays: 90);
                if (result.Success)
                {
                    return $"Deleted {result.Data} notifications";
                }
                return $"Failed: {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
