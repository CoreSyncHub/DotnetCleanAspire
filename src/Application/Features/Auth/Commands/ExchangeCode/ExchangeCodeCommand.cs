using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Dtos;
using Application.Features.Auth.Errors;

namespace Application.Features.Auth.Commands.ExchangeCode;

public sealed record ExchangeCodeCommand(string Code) : ICommand<AuthTokensDto>;

internal sealed class ExchangeCodeCommandHandler(
    IAuthCodeService authCodeService) : ICommandHandler<ExchangeCodeCommand, AuthTokensDto>
{
    public async Task<Result<AuthTokensDto>> Handle(
        ExchangeCodeCommand request,
        CancellationToken cancellationToken)
    {
        AuthTokensDto? tokens = await authCodeService.ExchangeCodeAsync(request.Code, cancellationToken);

        if (tokens is null)
        {
            return AuthErrors.InvalidOrExpiredCode;
        }

        return tokens;
    }
}
