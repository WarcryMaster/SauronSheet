namespace Application;

/// <summary>
/// Mock implementation of IUserContext for testing and Phase 0 development
/// </summary>
public class MockUserContext : IUserContext
{
    /// <summary>
    /// The mocked user ID (can be set for testing)
    /// </summary>
    public Guid UserId { get; set; } = Guid.NewGuid();
}
