namespace SauronSheet.Infrastructure.Auth;

/// <summary>
/// Auth Configuration class.
/// Binds to "Auth" section in appsettings.json.
/// Provides configurable values for cookie names, expiration, and JWT secret.
/// </summary>
public class AuthConfiguration
{
    public string AccessTokenCookieName { get; set; } = "sb-access-token";
    public string RefreshTokenCookieName { get; set; } = "sb-refresh-token";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public string JwtSecret { get; set; } = string.Empty;
}
