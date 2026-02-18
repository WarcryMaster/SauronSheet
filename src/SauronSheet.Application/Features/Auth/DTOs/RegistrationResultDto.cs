namespace SauronSheet.Application.Features.Auth.DTOs;

/// <summary>
/// DTO for user registration result.
/// Returned from RegisterUserCommandHandler.
/// </summary>
public record RegistrationResultDto(
    string UserId,
    string Email);
