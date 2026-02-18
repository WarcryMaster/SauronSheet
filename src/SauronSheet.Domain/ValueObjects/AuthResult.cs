namespace SauronSheet.Domain.ValueObjects;

/// <summary>
/// Auth operation result value object.
/// Represents the outcome of authentication operations (register, login, refresh token).
/// Uses factory methods for creation to ensure immutability and consistent state.
/// </summary>
public record AuthResult
{
    public UserId? UserId { get; }
    public string? AccessToken { get; }
    public string? RefreshToken { get; }
    public DateTime? ExpiresAt { get; }
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    private AuthResult(
        UserId? userId,
        string? accessToken,
        string? refreshToken,
        DateTime? expiresAt,
        bool isSuccess,
        string? errorMessage)
    {
        UserId = userId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static AuthResult Success(
        UserId userId,
        string accessToken,
        string refreshToken,
        DateTime expiresAt)
    {
        return new AuthResult(userId, accessToken, refreshToken, expiresAt, true, null);
    }

    public static AuthResult Failure(string errorMessage)
    {
        return new AuthResult(null, null, null, null, false, errorMessage);
    }
}
