using ErrorCodes = Application.Features.Auth.Errors.AuthErrors.Codes;

namespace Application.Features.Auth.Commands.RefreshToken;

internal sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.AccessTokenRequired);

        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.RefreshTokenRequired);
    }
}
