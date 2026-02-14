namespace Application;

/// <summary>
/// Marker interface for objects that should be scoped to a tenant (user)
/// Used to enforce multi-tenancy boundaries at the query level
/// </summary>
public interface ITenantScoped
{
    /// <summary>
    /// The ID of the tenant (user) that owns this resource
    /// </summary>
    Guid TenantId { get; }
}
