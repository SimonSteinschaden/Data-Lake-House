using System.Security.Claims;
using Enset.Application.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Enset.Api.Authorization;

public sealed class JwtCurrentUserEvents : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var externalIdentity = ExternalIdentityClaims.FindExternalIdentity(context.Principal!);
        if (string.IsNullOrWhiteSpace(externalIdentity))
        {
            context.Fail("The bearer token does not contain a supported external identity claim.");
            return;
        }

        var resolver = context.HttpContext.RequestServices
            .GetRequiredService<ICurrentUserResolver>();
        var resolved = await resolver.ResolveAsync(
            externalIdentity, context.HttpContext.RequestAborted);
        if (resolved is null)
        {
            context.Fail("The bearer token does not identify an active ENSET user.");
            return;
        }

        var currentUser = context.HttpContext.RequestServices
            .GetRequiredService<CurrentUserContext>();
        currentUser.Initialize(resolved.UserId, resolved.IsEnsetEmployee, resolved.Roles);

        if (context.Principal!.Identity is ClaimsIdentity identity)
        {
            foreach (var role in identity.FindAll(identity.RoleClaimType).ToArray())
                identity.RemoveClaim(role);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, resolved.UserId.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Name, resolved.ExternalIdentity));
            foreach (var role in resolved.Roles)
                identity.AddClaim(new Claim(identity.RoleClaimType, role));
        }
    }
}
