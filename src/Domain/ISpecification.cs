namespace Domain;

using System.Linq.Expressions;

/// <summary>
/// Specification pattern interface for querying entities with criteria and limitations
/// </summary>
/// <typeparam name="T">The entity type (must derive from Entity&lt;Guid&gt;)</typeparam>
public interface ISpecification<T> where T : Entity<Guid>
{
    /// <summary>
    /// The LINQ expression criteria for filtering entities
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    int MaxResults { get; }
}

/// <summary>
/// Base class for specifications providing a reusable pattern
/// </summary>
public abstract class Specification<T> : ISpecification<T> where T : Entity<Guid>
{
    /// <summary>
    /// The LINQ expression criteria for filtering entities
    /// </summary>
    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    /// <summary>
    /// Default maximum results (can be overridden by subclasses)
    /// </summary>
    public virtual int MaxResults => 1000;
}
