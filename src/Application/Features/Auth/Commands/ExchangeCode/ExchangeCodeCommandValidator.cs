using ErrorCodes = Application.Features.Auth.Errors.AuthErrors.Codes;

namespace Application.Features.Auth.Commands.ExchangeCode;

public sealed class ExchangeCodeCommandValidator : AbstractValidator<ExchangeCodeCommand>
{
    public ExchangeCodeCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.InvalidOrExpiredCode);
    }
}
