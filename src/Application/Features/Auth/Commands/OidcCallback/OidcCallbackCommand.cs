using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Dtos;
using Application.DependencyInjection.Options;
using Application.Features.Auth.Errors;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Application.Features.Auth.Commands.OidcCallback;

public sealed record OidcCallbackCommand(ClaimsPrincipal Principal) : ICommand<AuthTokensDto>;

internal sealed class OidcCallbackCommandHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    IOptions<OidcOptions> oidcOptions
    ) : ICommandHandler<OidcCallbackCommand, AuthTokensDto>
{
    public async Task<Result<AuthTokensDto>> Handle(
        OidcCallbackCommand request,
        CancellationToken cancellationToken)
    {
        ClaimsPrincipal principal = request.Principal;
        OidcOptions oidc = oidcOptions.Value;

        string? providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string? email = principal.FindFirstValue(ClaimTypes.Email);

        // Try configured claim type first, then fall back to standard claims
        string? name = principal.FindFirstValue(oidc.Provider.UsernameClaimType)
            ?? principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("name");

        if (string.IsNullOrEmpty(providerKey))
        {
            return AuthErrors.OidcAuthenticationFailed;
        }

        List<string> groups = [.. principal
            .FindAll(oidc.Provider.GroupClaimType)
            .Select(c => c.Value)];

        ExternalLoginInfo loginInfo = new(
            Provider: "oidc",
            ProviderKey: providerKey,
            Email: email,
            Name: name,
            Groups: groups);

        Result<UserDto> userResult = await identityService.GetOrCreateExternalUserAsync(loginInfo, cancellationToken);

        if (userResult.IsFailure)
        {
            return Result<AuthTokensDto>.Failure(userResult.Error);
        }

        UserDto user = userResult.Value;

        List<string> roles = [.. user.Roles];
        foreach (string group in groups)
        {
            if (oidc.Provider.GroupToRoleMapping.TryGetValue(group, out string? role) &&
                !roles.Contains(role))
            {
                roles.Add(role);
                await identityService.AddToRoleAsync(user.Id, role, cancellationToken);
            }
        }

        await identityService.UpdateLastLoginAsync(user.Id, cancellationToken);

        AuthTokensDto tokens = await tokenService.GenerateTokensAsync(
            user.Id,
            user.Email,
            roles,
            cancellationToken);

        return Result<AuthTokensDto>.Success(tokens);
    }
}
