namespace SauronSheet.Application.Features.Auth.DTOs;

/// <summary>
/// DTO for authentication tokens.
/// Returned from LoginUserCommandHandler and RefreshTokenCommandHandler.
/// </summary>
public record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserId);
