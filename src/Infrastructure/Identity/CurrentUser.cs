using Application.Abstractions.Helpers;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Identity;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : IUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Id? Id => ParseId(User?.FindFirstValue(ClaimTypes.NameIdentifier));

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles => User?
        .FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList() ?? [];

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    private static Id? ParseId(string? idValue)
    {
        if (idValue is null || !Domain.Abstractions.Id.TryParse(idValue, null, out Id id))
        {
            return null;
        }

        return id;
    }
}
