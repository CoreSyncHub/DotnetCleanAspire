using Application.Abstractions.Identity;

namespace Application.Features.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;

internal sealed class ForgotPasswordCommandHandler(
    IIdentityService identityService) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result<Unit>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken = default)
    {
        // Always return success to prevent email enumeration attacks
        // In a real application, you would send an email with the reset token
        Result<string> tokenResult = await identityService.GeneratePasswordResetTokenAsync(
            request.Email,
            cancellationToken);

        // TODO: Send email with reset token if successful
        // The token is available in tokenResult.Value when IsSuccess is true

        return Result.Success(SuccessType.NoContent);
    }
}
