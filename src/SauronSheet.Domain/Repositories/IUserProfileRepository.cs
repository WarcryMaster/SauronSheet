namespace SauronSheet.Domain.Repositories;

using ValueObjects;

/// <summary>
/// Repository interface for managing user profiles in public.users.
/// Ensures the application-level user profile exists before FK-constrained inserts.
/// </summary>
public interface IUserProfileRepository
{
    /// <summary>
    /// Upserts the user profile row in public.users.
    /// Safe to call multiple times (idempotent ON CONFLICT DO NOTHING).
    /// Needed because the Supabase trigger may not fire for existing auth users.
    /// </summary>
    Task EnsureExistsAsync(UserId userId, string email);
}
