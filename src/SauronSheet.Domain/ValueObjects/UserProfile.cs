namespace SauronSheet.Domain.ValueObjects;

using Exceptions;

/// <summary>
/// User profile value object.
/// Represents user profile information retrieved from authentication service.
/// Immutable data structure for displaying user information.
/// </summary>
public record UserProfile
{
    public UserId Id { get; }
    public string Email { get; }
    public string? DisplayName { get; }
    public DateTime CreatedAt { get; }

    public UserProfile(UserId id, string email, string? displayName, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be null or empty.");

        Id = id;
        Email = email;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }
}
