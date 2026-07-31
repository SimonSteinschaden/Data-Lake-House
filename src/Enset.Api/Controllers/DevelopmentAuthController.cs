using Enset.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class DevelopmentAuthController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly DevelopmentTokenService _tokens;

    public DevelopmentAuthController(
        IWebHostEnvironment environment,
        DevelopmentTokenService tokens)
    {
        _environment = environment;
        _tokens = tokens;
    }

    [AllowAnonymous]
    [HttpPost("development-token")]
    [ProducesResponseType(typeof(DevelopmentTokenResponse), StatusCodes.Status200OK)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<DevelopmentTokenResponse>> Create(
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var token = await _tokens.CreateAsync(cancellationToken);
        return token is null
            ? Problem(title: "Development user unavailable", statusCode: 503)
            : Ok(new DevelopmentTokenResponse(token.Value.Token, token.Value.ExpiresAt));
    }
}

public sealed record DevelopmentTokenResponse(string AccessToken, DateTime ExpiresAt);
