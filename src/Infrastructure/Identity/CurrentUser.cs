using Application.Abstractions.Helpers;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Identity;

/// <summary>
/// Provides access to the current user from the HTTP context.
/// </summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : IUser
{
   public string? Id => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
