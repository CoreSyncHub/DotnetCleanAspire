using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Dtos;
using Application.DependencyInjection.Options;
using Application.Features.Auth.Errors;
using Domain.Users.Constants;
using Microsoft.Extensions.Options;

namespace Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword) : ICommand<AuthTokensDto>;

internal sealed class RegisterCommandHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    IOptions<AuthIdentityOptions> identityOptions,
    IOptions<OidcOptions> oidcOptions) : ICommandHandler<RegisterCommand, AuthTokensDto>
{
    public async Task<Result<AuthTokensDto>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken = default)
    {
        OidcOptions oidc = oidcOptions.Value;
        if (oidc.Enabled && oidc.DisableLocalAuthWhenEnabled)
        {
            return AuthErrors.LocalAuthDisabled;
        }

        AuthIdentityOptions identity = identityOptions.Value;
        if (!identity.EnableLocalRegistration)
        {
            return AuthErrors.RegistrationDisabled;
        }

        Result<Id> createResult = await identityService.CreateUserAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (createResult.IsFailure)
        {
            return Result.Failure<AuthTokensDto>(createResult.Error);
        }

        Id userId = createResult.Value;

        await identityService.AddToRoleAsync(userId, Roles.User, cancellationToken);

        IReadOnlyList<string> roles = [Roles.User];

        AuthTokensDto tokens = await tokenService.GenerateTokensAsync(
            userId,
            request.Email,
            roles,
            cancellationToken);

        return Result.Success(tokens, SuccessType.Created);
    }
}
