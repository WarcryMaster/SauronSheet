namespace Application.Common;

/// <summary>
/// Base data transfer object implementing tenant scoping
/// </summary>
public abstract class BaseDto : ITenantScoped
{
    /// <summary>
    /// The unique identifier of the resource
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the tenant (user) that owns this resource
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// When the resource was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
