namespace SauronSheet.Application.Common.Behaviors;

using MediatR;

/// <summary>
/// MediatR pipeline behavior for tenant scoping.
/// Enforces authentication for all requests except those marked with IAnonymousRequest.
/// Runs before handlers to protect authenticated endpoints.
/// </summary>
public class TenantScopingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUserContext _userContext;

    public TenantScopingBehavior(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip auth check for anonymous requests (Register, Login, RefreshToken)
        if (request is IAnonymousRequest)
            return await next();

        // Enforce authentication for all other requests
        if (!_userContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await next();
    }
}
