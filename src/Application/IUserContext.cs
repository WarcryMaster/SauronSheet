namespace Application;

/// <summary>
/// Interface for accessing current user context information
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// The ID of the current user
    /// </summary>
    Guid UserId { get; }
}
