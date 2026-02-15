using ErrorCodes = Application.Features.Auth.Errors.AuthErrors.Codes;

namespace Application.Features.Auth.Commands.Login;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.EmailRequired)
            .EmailAddress()
            .WithErrorCode(ErrorCodes.InvalidEmail);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.PasswordRequired);
    }
}
