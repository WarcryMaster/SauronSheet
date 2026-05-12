namespace SauronSheet.Domain.Common;

/// <summary>
/// Contract for accessing current authenticated user context.
/// Resolved from HTTP context in the presentation layer.
/// Injected into handlers and repositories for tenant scoping.
/// Defined in Domain layer as a cross-cutting contract.
/// </summary>
public interface IUserContext
{
    string UserId { get; }
    string UserEmail { get; }
    bool IsAuthenticated { get; }
}
