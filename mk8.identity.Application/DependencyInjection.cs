using mk8.identity.Application.Services;
using mk8.identity.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Application
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers all application layer services with the dependency injection container.
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register services - order matters for dependency resolution
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IMatrixAccountService, MatrixAccountService>();
            services.AddScoped<IPrivilegesService, PrivilegesService>();
            services.AddScoped<IMembershipService, MembershipService>();
            services.AddScoped<IContributionService, ContributionService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IUserService, UserService>();

            // Register background job service
            services.AddScoped<DailyJobService>();

            return services;
        }
    }
}
