using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Enset.Application.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Enset.Api.Authorization;

public sealed class DevelopmentTokenService
{
    public const string DevelopmentExternalIdentity = "development-user";
    private readonly JwtOptions _options;
    private readonly ICurrentUserResolver _users;

    public DevelopmentTokenService(IOptions<JwtOptions> options, ICurrentUserResolver users)
    {
        _options = options.Value;
        _users = users;
    }

    public async Task<(string Token, DateTime ExpiresAt)?> CreateAsync(
        CancellationToken cancellationToken)
    {
        var user = await _users.ResolveAsync(DevelopmentExternalIdentity, cancellationToken);
        if (user is null)
            return null;

        var expires = DateTime.UtcNow.AddMinutes(_options.DevelopmentTokenLifetimeMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expires,
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, user.ExternalIdentity),
                new Claim(ClaimTypes.Name, user.ExternalIdentity)
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };
        var handler = new JwtSecurityTokenHandler();
        return (handler.WriteToken(handler.CreateToken(descriptor)), expires);
    }
}
