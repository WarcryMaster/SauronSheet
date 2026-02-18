namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;

/// <summary>
/// Supabase implementation of IPdfImportRepository.
/// CRITICAL FIX C-2: Interface defined in Domain layer.
/// </summary>
public class SupabasePdfImportRepository : IPdfImportRepository
{
    private readonly Supabase.Client _client;

    public SupabasePdfImportRepository(Supabase.Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task AddAsync(ImportBatch importBatch)
    {
        // TODO Phase 3F: Implement Supabase insert
        // INSERT INTO pdf_imports (id, user_id, filename, imported_count, skipped_count, imported_at)
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase AddAsync");
    }

    public async Task<IReadOnlyList<ImportBatch>> GetByUserIdAsync(UserId userId)
    {
        // TODO Phase 3F: Implement Supabase query
        // SELECT * FROM pdf_imports WHERE user_id = userId.Value ORDER BY imported_at DESC
        throw new NotImplementedException("TODO Phase 3F: Implement Supabase GetByUserIdAsync");
    }
}
