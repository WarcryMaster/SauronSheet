namespace SauronSheet.Infrastructure.Auth;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using SauronSheet.Domain.Services;

/// <summary>
/// JWT Cookie Middleware.
/// Reads JWT from HTTP-only cookie, validates signature using Supabase JWKS public keys,
/// and sets ClaimsPrincipal on HttpContext.
/// Supabase uses ES256 (ECDSA P-256) for JWT signing with asymmetric key pairs.
/// Public keys are fetched from the JWKS endpoint at startup.
/// </summary>
public class JwtCookieMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthConfiguration _config;
    private readonly ILogger<JwtCookieMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly JwtSecurityTokenHandler _handler;
    private readonly TokenValidationParameters _validationParameters;

    public JwtCookieMiddleware(
        RequestDelegate next,
        IOptions<AuthConfiguration> options,
        ILogger<JwtCookieMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _config = options.Value;
        _logger = logger;
        _environment = environment;
        _handler = new JwtSecurityTokenHandler
        {
            // Disable default claim type mapping so JWT claims like "sub", "email"
            // retain their original names instead of being mapped to long XML URIs.
            MapInboundClaims = false
        };

        if (environment.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
        }

        // Supabase uses ES256 (asymmetric) JWT signing.
        // Fetch public keys from the JWKS endpoint for signature verification.
        IList<SecurityKey> signingKeys = FetchJwksSigningKeys();

        _validationParameters = new TokenValidationParameters
        {
            IssuerSigningKeys = signingKeys,
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
                var principal = _handler.ValidateToken(token, _validationParameters, out _);
                context.User = principal;
                _logger.LogDebug("JWT validated for user {UserId}", principal.FindFirst("sub")?.Value);
            }
            catch (SecurityTokenExpiredException)
            {
                // Token signature is valid but expired — try to refresh
                _logger.LogDebug("JWT expired for request {Path}, attempting refresh", context.Request.Path);
                await TryRefreshTokenAsync(context);
            }
            catch (SecurityTokenException ex)
            {
                // Signature invalid, issuer mismatch, or other validation failure — do NOT refresh
                _logger.LogWarning(ex, "JWT validation failed for access token cookie (not expired)");
            }
        }
        else
        {
            _logger.LogDebug("No access token cookie for request {Path}", context.Request.Path);
        }

        await _next(context);
    }

    /// <summary>
    /// Attempts to refresh an expired access token using the refresh token cookie.
    /// On success: sets new access token and refresh token cookies, validates new JWT.
    /// On failure: clears both auth cookies (user must re-login).
    /// </summary>
    private async Task TryRefreshTokenAsync(HttpContext context)
    {
        var refreshToken = context.Request.Cookies[_config.RefreshTokenCookieName];

        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogInformation("JWT expired but no refresh token cookie found — user must re-login");
            ClearAuthCookies(context);
            return;
        }

        try
        {
            var authService = context.RequestServices.GetRequiredService<IAuthService>();
            var result = await authService.RefreshTokenAsync(refreshToken);

            if (!result.IsSuccess)
            {
                _logger.LogInformation(
                    "Refresh token rejected for user {UserId}: {Error}",
                    result.UserId?.Value ?? "unknown",
                    result.ErrorMessage);
                ClearAuthCookies(context);
                return;
            }

            // Refresh succeeded — set new cookies
            bool useSecureCookies = !_environment.IsDevelopment();

            // New access token cookie (expires when JWT expires)
            context.Response.Cookies.Append(
                _config.AccessTokenCookieName,
                result.AccessToken!,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = useSecureCookies,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = result.ExpiresAt
                });

            // New refresh token cookie (7 days — Supabase rotates refresh tokens)
            context.Response.Cookies.Append(
                _config.RefreshTokenCookieName,
                result.RefreshToken!,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = useSecureCookies,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

            // Validate the NEW token and set the user principal for this request
            var principal = _handler.ValidateToken(result.AccessToken!, _validationParameters, out _);
            context.User = principal;

            _logger.LogInformation(
                "JWT auto-refreshed for user {UserId}",
                principal.FindFirst("sub")?.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during JWT refresh");
            ClearAuthCookies(context);
        }
    }

    /// <summary>
    /// Clears both access token and refresh token cookies.
    /// </summary>
    private void ClearAuthCookies(HttpContext context)
    {
        context.Response.Cookies.Delete(_config.AccessTokenCookieName, new CookieOptions { Path = "/" });
        context.Response.Cookies.Delete(_config.RefreshTokenCookieName, new CookieOptions { Path = "/" });
    }

    private IList<SecurityKey> FetchJwksSigningKeys()
    {
        var jwksUrl = _config.SupabaseIssuer.TrimEnd('/') + "/.well-known/jwks.json";

        try
        {
            using var httpClient = new HttpClient();
            var jwksJson = Task.Run(() => httpClient.GetStringAsync(jwksUrl))
                .GetAwaiter().GetResult();
            var jwks = new JsonWebKeySet(jwksJson);
            IList<SecurityKey> keys = jwks.GetSigningKeys();

            _logger.LogInformation(
                "Loaded {KeyCount} signing key(s) from Supabase JWKS endpoint", keys.Count);

            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch JWKS from {Url}. JWT validation will fail", jwksUrl);
            return [];
        }
    }
}
