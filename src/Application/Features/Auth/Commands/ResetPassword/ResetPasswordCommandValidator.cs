using ErrorCodes = Application.Features.Auth.Errors.AuthErrors.Codes;

namespace Application.Features.Auth.Commands.ResetPassword;

internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.EmailRequired)
            .EmailAddress()
            .WithErrorCode(ErrorCodes.InvalidEmail);

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.ResetTokenRequired);

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.PasswordRequired)
            .MinimumLength(8)
            .WithErrorCode(ErrorCodes.PasswordTooShort);

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithErrorCode(ErrorCodes.PasswordsDoNotMatch);
    }
}
