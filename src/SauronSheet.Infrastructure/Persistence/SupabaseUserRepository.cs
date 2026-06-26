namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Threading.Tasks;
using Postgrest.Attributes;
using Postgrest.Models;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Postgrest DTO for the public.users table.
/// </summary>
[Table("users")]
internal class UserRow : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = "";

    [Column("email")]
    public string Email { get; set; } = "";
}

/// <summary>
/// Supabase implementation of IUserProfileRepository.
/// Upserts the user's profile row into public.users to satisfy FK constraints
/// on transactions and other tables that reference public.users(id).
/// This is a resilience measure: normally the trigger handle_new_user keeps
/// auth.users and public.users in sync, but we cannot rely on it alone.
/// </summary>
public class SupabaseUserRepository : IUserProfileRepository
{
    private readonly Supabase.Client _client;

    public SupabaseUserRepository(Supabase.Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task EnsureExistsAsync(UserId userId, string email)
    {
        var row = new UserRow
        {
            Id = userId.Value,
            Email = email
        };

        // Upsert: inserts if not exists, ignores conflict on id (idempotent).
        await _client.From<UserRow>()
            .Upsert(row);

        Sentry.SentrySdk.Logger?.LogDebug(
            "SupabaseUserRepository.EnsureExistsAsync: upserted user profile for {0}", userId.Value);
    }
}
