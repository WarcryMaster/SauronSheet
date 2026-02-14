namespace Application.Behaviors;

using MediatR;

/// <summary>
/// MediatR pipeline behavior for enforcing multi-tenancy boundaries
/// Validates that query responses are scoped to the current user's tenant
/// </summary>
public class ScopedQueryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUserContext _userContext;

    public ScopedQueryBehavior(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        // Validate that the response respects tenant boundaries
        if (response is ITenantScoped tenantScoped)
        {
            if (tenantScoped.TenantId != _userContext.UserId)
                throw new UnauthorizedAccessException(
                    $"Tenant boundary violation: User {_userContext.UserId} attempted to access resource for tenant {tenantScoped.TenantId}"
                );
        }

        return response;
    }
}
