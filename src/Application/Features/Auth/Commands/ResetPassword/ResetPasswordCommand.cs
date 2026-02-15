using Application.Abstractions.Identity;

namespace Application.Features.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword) : ICommand;

internal sealed class ResetPasswordCommandHandler(
    IIdentityService identityService) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result<Unit>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        return await identityService.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword,
            cancellationToken);
    }
}
