namespace SauronSheet.Domain.Services;

using ValueObjects;

/// <summary>
/// Auth service interface - Domain layer contract.
/// Defines the contract that Infrastructure must implement.
/// Application layer depends on this abstraction, not the implementation.
/// Implementation: SupabaseAuthService in Infrastructure.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user with email and password.
    /// Returns AuthResult with success/failure details.
    /// </summary>
    Task<AuthResult> RegisterAsync(string email, string password);

    /// <summary>
    /// Resend account confirmation email for a pending signup.
    /// Returns AuthResult with success/failure details.
    /// </summary>
    Task<AuthResult> ResendConfirmationEmailAsync(string email);

    /// <summary>
    /// Login an existing user with email and password.
    /// Returns AuthResult with JWT tokens on success.
    /// </summary>
    Task<AuthResult> LoginAsync(string email, string password);

    /// <summary>
    /// Logout a user by revoking their session.
    /// Accepts the access token for the session to revoke.
    /// </summary>
    Task LogoutAsync(string accessToken);

    /// <summary>
    /// Refresh an expired access token using the refresh token.
    /// Returns new tokens on success.
    /// </summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Get user profile information by user ID.
    /// Returns null if user not found.
    /// </summary>
    Task<UserProfile?> GetUserProfileAsync(string userId);
}
