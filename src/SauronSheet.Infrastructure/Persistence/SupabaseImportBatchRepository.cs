namespace SauronSheet.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Postgrest;
using Postgrest.Attributes;
using Postgrest.Models;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;

/// <summary>
/// Postgrest DTO for the <c>import_batches</c> table.
/// Table was renamed from <c>pdf_imports</c> via migration 012_RenamePdfImportsToImportBatches.sql.
/// </summary>
[Table("import_batches")]
internal class ImportBatchRow : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("filename")]
    public string Filename { get; set; } = string.Empty;

    [Column("imported_count")]
    public int ImportedCount { get; set; }

    [Column("skipped_count")]
    public int SkippedCount { get; set; }

    [Column("imported_at")]
    public DateTime ImportedAt { get; set; }

    public ImportBatch ToDomain() =>
        new(Guid.Parse(Id), Filename, ImportedCount, SkippedCount, ImportedAt);

    public static ImportBatchRow FromDomain(ImportBatch batch, string userId) =>
        new()
        {
            Id = batch.Id.ToString(),
            UserId = userId,
            Filename = batch.Filename,
            ImportedCount = batch.ImportedCount,
            SkippedCount = batch.SkippedCount,
            ImportedAt = batch.ImportedAt,
        };
}

/// <summary>
/// Supabase implementation of <see cref="IImportBatchRepository"/>.
/// Persists import-batch metadata to the <c>import_batches</c> table (scoped per user via RLS).
/// </summary>
public class SupabaseImportBatchRepository : IImportBatchRepository
{
    private readonly Supabase.Client _client;

    public SupabaseImportBatchRepository(Supabase.Client client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public async Task AddAsync(ImportBatch importBatch, UserId userId)
    {
        var row = ImportBatchRow.FromDomain(importBatch, userId.Value);
        await _client.From<ImportBatchRow>().Insert(row);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportBatch>> GetByUserIdAsync(UserId userId)
    {
        var response = await _client.From<ImportBatchRow>()
            .Where(x => x.UserId == userId.Value)
            .Order("imported_at", Constants.Ordering.Descending)
            .Get();

        return response.Models.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }
}
