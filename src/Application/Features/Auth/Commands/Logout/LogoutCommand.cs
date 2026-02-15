using Application.Abstractions.Helpers;
using Application.Abstractions.Identity;

namespace Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(string? RefreshToken = null) : ICommand;

internal sealed class LogoutCommandHandler(
    ITokenService tokenService,
    IUser currentUser) : ICommandHandler<LogoutCommand>
{
    public async Task<Result<Unit>> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            // Result intentionally discarded: an invalid token means the user is already effectively logged out
            _ = await tokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        }
        else if (currentUser.Id is not null)
        {
            _ = await tokenService.RevokeAllUserTokensAsync((Id)currentUser.Id, cancellationToken);
        }

        return Result.Success(SuccessType.NoContent);
    }
}
