using Application.Abstractions.DependencyInjection;
using Application.Abstractions.Identity;
using Application.DependencyInjection.Options;
using Infrastructure.Identity.Entities;
using Infrastructure.Identity.Services;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.DependencyInjection;

internal static partial class InfrastructureDependencyInjection
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddIdentityServices()
        {
            IConfiguration configuration = builder.Configuration;

            builder.Services.AddOptionsWithValidation<AuthIdentityOptions, AuthIdentityOptionsValidator>(
                configuration,
                AuthIdentityOptions.SectionName);

            AuthIdentityOptions identityConfig = configuration
                .GetSection(AuthIdentityOptions.SectionName)
                .Get<AuthIdentityOptions>() ?? new();

            // Use AddIdentityCore instead of AddIdentity to avoid cookie authentication
            // which would override JWT as the default scheme and redirect to /Account/Login
            builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = identityConfig.PasswordPolicy.RequireDigit;
                options.Password.RequireLowercase = identityConfig.PasswordPolicy.RequireLowercase;
                options.Password.RequireUppercase = identityConfig.PasswordPolicy.RequireUppercase;
                options.Password.RequireNonAlphanumeric = identityConfig.PasswordPolicy.RequireNonAlphanumeric;
                options.Password.RequiredLength = identityConfig.PasswordPolicy.MinimumLength;

                options.Lockout.MaxFailedAccessAttempts = identityConfig.Lockout.MaxFailedAttempts;
                options.Lockout.DefaultLockoutTimeSpan = identityConfig.Lockout.LockoutDuration;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = string.Empty; // Allow any character in username
                options.SignIn.RequireConfirmedEmail = identityConfig.RequireEmailConfirmation;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddSignInManager();

            builder.Services.AddScoped<IIdentityService, IdentityService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthCodeService, AuthCodeService>();

            return builder;
        }
    }
}
