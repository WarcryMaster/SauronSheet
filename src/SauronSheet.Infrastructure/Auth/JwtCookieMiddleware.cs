namespace SauronSheet.Infrastructure.Auth;

using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

/// <summary>
/// JWT Cookie Middleware.
/// Reads JWT from HTTP-only secure cookie and sets ClaimsPrincipal on HttpContext.
/// Runs before authentication/authorization middleware to extract user claims.
/// </summary>
public class JwtCookieMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthConfiguration _config;

    public JwtCookieMiddleware(RequestDelegate next, AuthConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Cookies[_config.AccessTokenCookieName];

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                // Parse JWT without signature validation (Supabase already validated it)
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Extract "sub" claim (Supabase standard claim for user ID)
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    var claims = new List<Claim>
                    {
                        new Claim("sub", userId),
                        new Claim(ClaimTypes.Email, email ?? string.Empty)
                    };

                    var identity = new ClaimsIdentity(claims, "jwt");
                    var principal = new ClaimsPrincipal(identity);
                    context.User = principal;
                }
            }
            catch
            {
                // Invalid token - user remains unauthenticated
            }
        }

        await _next(context);
    }
}
