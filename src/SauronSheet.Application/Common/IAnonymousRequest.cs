namespace SauronSheet.Application.Common;

/// <summary>
/// Marker interface for requests that do not require authentication.
/// Applied to: RegisterUserCommand, LoginUserCommand, RefreshTokenCommand
/// Used by TenantScopingBehavior to skip authentication checks for these operations.
/// </summary>
public interface IAnonymousRequest
{
}
