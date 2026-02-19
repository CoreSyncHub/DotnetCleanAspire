using System.Security.Claims;
using Application.DependencyInjection.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity.Handlers;

internal sealed class OidcClaimsTransformer(IOptions<OidcOptions> oidcOptions) : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        OidcOptions config = oidcOptions.Value;

        if (!config.Enabled || principal.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult(principal);
        }

        if (principal.Identity is not ClaimsIdentity identity)
        {
            return Task.FromResult(principal);
        }

        var groupClaims = principal.FindAll(config.Provider.GroupClaimType).ToList();

        foreach (Claim groupClaim in groupClaims)
        {
            if (config.Provider.GroupToRoleMapping.TryGetValue(groupClaim.Value, out string? roleName)
                && !principal.IsInRole(roleName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
            }
        }

        return Task.FromResult(principal);
    }
}
