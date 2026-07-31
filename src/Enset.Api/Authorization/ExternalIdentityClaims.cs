using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Enset.Api.Authorization;

public static class ExternalIdentityClaims
{
    public static string? FindExternalIdentity(ClaimsPrincipal principal) =>
        principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
        principal.FindFirstValue("oid") ??
        principal.FindFirstValue(ClaimTypes.NameIdentifier);
}
