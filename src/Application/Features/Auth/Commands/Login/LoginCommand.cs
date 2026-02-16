using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Dtos;
using Application.DependencyInjection.Options;
using Application.Features.Auth.Errors;
using Microsoft.Extensions.Options;

namespace Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<AuthTokensDto>;

internal sealed class LoginCommandHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    IOptions<OidcOptions> oidcOptions) : ICommandHandler<LoginCommand, AuthTokensDto>
{
    public async Task<Result<AuthTokensDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken = default)
    {
        OidcOptions oidc = oidcOptions.Value;
        if (oidc.Enabled && oidc.DisableLocalAuthWhenEnabled)
        {
            return AuthErrors.LocalAuthDisabled;
        }

        Result<Unit> validateResult = await identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (validateResult.IsFailure)
        {
            return Result.Failure<AuthTokensDto>(validateResult.Error);
        }

        UserDto? user = await identityService.GetUserByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        await identityService.UpdateLastLoginAsync(user.Id, cancellationToken);

        AuthTokensDto tokens = await tokenService.GenerateTokensAsync(
            user.Id,
            user.Email,
            user.Roles,
            cancellationToken);

        return tokens;
    }
}
