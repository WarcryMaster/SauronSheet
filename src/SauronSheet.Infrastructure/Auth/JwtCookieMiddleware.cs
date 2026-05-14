namespace SauronSheet.Infrastructure.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// JWT Cookie Middleware.
/// Reads JWT from HTTP-only secure cookie, validates signature and expiration,
/// and sets ClaimsPrincipal on HttpContext.
/// Runs before authentication/authorization middleware to extract user claims.
/// </summary>
public class JwtCookieMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthConfiguration _config;
    private readonly ILogger<JwtCookieMiddleware> _logger;
    private readonly JwtSecurityTokenHandler _handler;
    private readonly TokenValidationParameters _validationParameters;

    public JwtCookieMiddleware(
        RequestDelegate next,
        IOptions<AuthConfiguration> options,
        ILogger<JwtCookieMiddleware> logger)
    {
        _next = next;
        _config = options.Value;
        _logger = logger;
        _handler = new JwtSecurityTokenHandler();

        var keyBytes = Encoding.UTF8.GetBytes(_config.JwtSecret);
        var key = new SymmetricSecurityKey(keyBytes);

        _validationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = key,
            ValidIssuer = _config.SupabaseIssuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            AuthenticationType = "jwt"
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Cookies[_config.AccessTokenCookieName];

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var principal = _handler.ValidateToken(token, _validationParameters, out var validatedToken);
                context.User = principal;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "JWT validation failed for access token cookie");
            }
        }

        await _next(context);
    }
}
