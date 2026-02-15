using ErrorCodes = Application.Features.Auth.Errors.AuthErrors.Codes;

namespace Application.Features.Auth.Commands.ForgotPassword;

internal sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.EmailRequired)
            .EmailAddress()
            .WithErrorCode(ErrorCodes.InvalidEmail);
    }
}
