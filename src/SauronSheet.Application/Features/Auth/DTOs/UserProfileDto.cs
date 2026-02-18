namespace SauronSheet.Application.Features.Auth.DTOs;

/// <summary>
/// DTO for current user profile information.
/// Returned from GetCurrentUserQueryHandler.
/// </summary>
public record UserProfileDto(
    string UserId,
    string Email,
    string? DisplayName,
    DateTime CreatedAt);
