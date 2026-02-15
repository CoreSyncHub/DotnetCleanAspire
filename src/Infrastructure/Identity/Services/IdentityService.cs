using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Dtos;
using Application.Features.Auth.Errors;
using Domain.Abstractions;
using Domain.Users.Constants;
using Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ExternalLoginInfo = Application.Abstractions.Identity.Dtos.ExternalLoginInfo;

namespace Infrastructure.Identity.Services;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : IIdentityService
{
    /// <inheritdoc/>
    public async Task<Result<Id>> CreateUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return AuthErrors.EmailAlreadyExists;
        }

        ApplicationUser user = new()
        {
            UserName = email,
            Email = email
        };

        IdentityResult result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new ResultError("Auth.CreateFailed", errors, ErrorType.Validation);
        }

        return user.Id;
    }

    /// <inheritdoc/>
    public async Task<Result<Unit>> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return AuthErrors.AccountLocked;
        }

        bool isValid = await userManager.CheckPasswordAsync(user, password);
        if (!isValid)
        {
            await userManager.AccessFailedAsync(user);
            return AuthErrors.InvalidCredentials;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result<Unit>> ConfirmEmailAsync(
        Id userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        IdentityResult result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return AuthErrors.InvalidToken;
        }

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result<string>> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        string token = await userManager.GeneratePasswordResetTokenAsync(user);
        return token;
    }

    /// <inheritdoc/>
    public async Task<Result<Unit>> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        IdentityResult result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new ResultError("Auth.ResetPasswordFailed", errors, ErrorType.Validation);
        }

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result<Unit>> AddToRoleAsync(
        Id userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        if (await userManager.IsInRoleAsync(user, role))
        {
            return Result.Success();
        }

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = role });
        }

        IdentityResult result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new ResultError("Auth.AddToRoleFailed", errors, ErrorType.Failure);
        }

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<bool> IsInRoleAsync(
        Id userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && await userManager.IsInRoleAsync(user, role);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetRolesAsync(
        Id userId,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return [];
        }

        IList<string> roles = await userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    /// <inheritdoc/>
    public async Task<UserDto?> GetUserByIdAsync(
        Id userId,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        return await ToUserDtoAsync(user);
    }

    /// <inheritdoc/>
    public async Task<UserDto?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        return await ToUserDtoAsync(user);
    }

    /// <inheritdoc/>
    public async Task<Result<UserDto>> GetOrCreateExternalUserAsync(
        ExternalLoginInfo loginInfo,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.Users
            .FirstOrDefaultAsync(u =>
                u.ExternalProvider == loginInfo.Provider &&
                u.ExternalProviderKey == loginInfo.ProviderKey,
                cancellationToken);

        if (user is not null)
        {
            return await ToUserDtoAsync(user);
        }

        if (!string.IsNullOrEmpty(loginInfo.Email))
        {
            user = await userManager.FindByEmailAsync(loginInfo.Email);
            if (user is not null)
            {
                user.ExternalProvider = loginInfo.Provider;
                user.ExternalProviderKey = loginInfo.ProviderKey;
                await userManager.UpdateAsync(user);
                return await ToUserDtoAsync(user);
            }
        }

        user = new ApplicationUser
        {
            UserName = loginInfo.Name ?? loginInfo.Email ?? $"{loginInfo.Provider}_{loginInfo.ProviderKey}",
            Email = loginInfo.Email,
            EmailConfirmed = !string.IsNullOrEmpty(loginInfo.Email),
            ExternalProvider = loginInfo.Provider,
            ExternalProviderKey = loginInfo.ProviderKey
        };

        IdentityResult result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new ResultError("Auth.ExternalLoginFailed", errors, ErrorType.Failure);
        }

        await userManager.AddToRoleAsync(user, Roles.User);

        return await ToUserDtoAsync(user);
    }

    /// <inheritdoc/>
    public async Task<Result<Unit>> LinkExternalLoginAsync(
        Id userId,
        string provider,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        user.ExternalProvider = provider;
        user.ExternalProviderKey = providerKey;

        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new ResultError("Auth.LinkExternalLoginFailed", errors, ErrorType.Failure);
        }

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result<Unit>> UpdateLastLoginAsync(
        Id userId,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        return Result.Success();
    }

    /// <inheritdoc/>
    private async Task<UserDto> ToUserDtoAsync(ApplicationUser user)
    {
        IList<string> roles = await userManager.GetRolesAsync(user);
        return new UserDto(
            user.Id,
            user.Email!,
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            roles.ToList());
    }
}
