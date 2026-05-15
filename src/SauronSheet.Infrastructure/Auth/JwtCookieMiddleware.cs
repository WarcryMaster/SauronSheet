namespace SauronSheet.Infrastructure.Auth;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

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
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "JWT validation failed for access token cookie");
            }
        }
        else
        {
            _logger.LogDebug("No access token cookie for request {Path}", context.Request.Path);
        }

        await _next(context);
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
