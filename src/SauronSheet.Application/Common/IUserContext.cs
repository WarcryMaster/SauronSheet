namespace SauronSheet.Application.Common;

/// <summary>
/// Contract for accessing current authenticated user context.
/// Resolved from HTTP context in Frontend layer.
/// Injected into handlers for tenant scoping.
/// Implementation provided in Phase 1.
/// </summary>
public interface IUserContext
{
    string UserId { get; }
    bool IsAuthenticated { get; }
}
