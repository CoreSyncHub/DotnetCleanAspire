using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Dtos;

namespace Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken) : ICommand<AuthTokensDto>;

internal sealed class RefreshTokenCommandHandler(
    ITokenService tokenService) : ICommandHandler<RefreshTokenCommand, AuthTokensDto>
{
    public async Task<Result<AuthTokensDto>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken = default)
    {
        return await tokenService.RefreshTokensAsync(
            request.AccessToken,
            request.RefreshToken,
            cancellationToken);
    }
}
