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
/// Postgrest DTO for the pdf_imports table.
/// CRITICAL FIX I-2: Table name is pdf_imports (NOT import_batches).
/// </summary>
[Table("pdf_imports")]
internal class PdfImportRow : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = "";

    [Column("user_id")]
    public string UserId { get; set; } = "";

    [Column("filename")]
    public string Filename { get; set; } = "";

    [Column("imported_count")]
    public int ImportedCount { get; set; }

    [Column("skipped_count")]
    public int SkippedCount { get; set; }

    [Column("imported_at")]
    public DateTime ImportedAt { get; set; }

    public ImportBatch ToDomain()
    {
        return new ImportBatch(
            Guid.Parse(Id),
            Filename,
            ImportedCount,
            SkippedCount,
            ImportedAt);
    }

    public static PdfImportRow FromDomain(ImportBatch batch, string userId)
    {
        return new PdfImportRow
        {
            Id = batch.Id.ToString(),
            UserId = userId,
            Filename = batch.Filename,
            ImportedCount = batch.ImportedCount,
            SkippedCount = batch.SkippedCount,
            ImportedAt = batch.ImportedAt
        };
    }
}

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

    public async Task AddAsync(ImportBatch importBatch, UserId userId)
    {
        try
        {
            var row = PdfImportRow.FromDomain(importBatch, userId.Value);
            await _client.From<PdfImportRow>().Insert(row);
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabasePdfImportRepository.AddAsync");
                scope.SetTag("userId", userId.Value);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }

    public async Task<IReadOnlyList<ImportBatch>> GetByUserIdAsync(UserId userId)
    {
        try
        {
            var response = await _client.From<PdfImportRow>()
                .Where(x => x.UserId == userId.Value)
                .Order("imported_at", Constants.Ordering.Descending)
                .Get();

            return response.Models.Select(r => r.ToDomain()).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope => {
                scope.SetTag("repo", "SupabasePdfImportRepository.GetByUserIdAsync");
                scope.SetTag("userId", userId.Value);
                scope.Level = Sentry.SentryLevel.Error;
            });
            throw;
        }
    }
}
