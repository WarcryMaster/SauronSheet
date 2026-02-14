namespace Domain;

/// <summary>
/// Generic repository interface for persisting and retrieving domain entities.
/// Abstracts the persistence mechanism from the domain logic.
/// </summary>
/// <typeparam name="T">The entity type (must derive from Entity&lt;Guid&gt;)</typeparam>
public interface IRepository<T> where T : Entity<Guid>
{
    /// <summary>
    /// Add a new entity to the repository
    /// </summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing entity in the repository
    /// </summary>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an entity from the repository
    /// </summary>
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an entity by its ID
    /// </summary>
    /// <returns>The entity if found; otherwise null</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all entities from the repository
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entities matching a specification
    /// </summary>
    Task<IEnumerable<T>> GetBySpecificationAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
}
