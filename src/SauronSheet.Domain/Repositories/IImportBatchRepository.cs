namespace SauronSheet.Domain.Repositories;

using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;
using ValueObjects;

/// <summary>
/// Repository for tracking statement import batch metadata.
/// Neutral replacement for <c>IPdfImportRepository</c> — stores metadata for any import
/// regardless of source format (Excel, CSV, etc.).
/// </summary>
public interface IImportBatchRepository
{
    /// <summary>Persists a new import batch record for the given user.</summary>
    Task AddAsync(ImportBatch importBatch, UserId userId);

    /// <summary>Returns all import batches for the given user, ordered by import date descending.</summary>
    Task<IReadOnlyList<ImportBatch>> GetByUserIdAsync(UserId userId);
}
