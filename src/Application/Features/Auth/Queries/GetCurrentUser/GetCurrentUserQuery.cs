using Application.Abstractions.Helpers;
using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Dtos;
using Application.Features.Auth.Errors;

namespace Application.Features.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<UserDto>;

internal sealed class GetCurrentUserQueryHandler(
    IIdentityService identityService,
    IUser currentUser) : IQueryHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<Result<UserDto>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.Id is null)
        {
            return AuthErrors.UserNotAuthenticated;
        }

        UserDto? user = await identityService.GetUserByIdAsync((Id)currentUser.Id, cancellationToken);

        if (user is null)
        {
            return AuthErrors.UserNotAuthenticated;
        }

        return Result<UserDto>.Success(user);
    }
}
