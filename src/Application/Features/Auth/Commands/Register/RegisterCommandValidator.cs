using Application.DependencyInjection.Options;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using ErrorCodes = Application.Features.Auth.Errors.AuthErrors.Codes;

namespace Application.Features.Auth.Commands.Register;

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator(IOptions<AuthIdentityOptions> identityOptions)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.EmailRequired)
            .EmailAddress()
            .WithErrorCode(ErrorCodes.InvalidEmail);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.PasswordRequired)
            .MinimumLength(identityOptions.Value.PasswordPolicy.MinimumLength)
            .WithErrorCode(ErrorCodes.PasswordTooShort);

        RuleFor(x => x.ConfirmPassword)
            .Custom((confirmPassword, context) =>
            {
                string password = context.InstanceToValidate.Password;
                if (confirmPassword != password)
                {
                    context.AddFailure(new ValidationFailure(nameof(RegisterCommand.ConfirmPassword), "Passwords do not match")
                    {
                        ErrorCode = ErrorCodes.PasswordsDoNotMatch
                    });
                }
            });
    }
}
