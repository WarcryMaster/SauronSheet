namespace SauronSheet.Domain.Repositories;

using Entities;
using ValueObjects;

/// <summary>
/// Repository for tracking PDF import metadata.
/// CRITICAL FIX C-2: Moved from Infrastructure to Domain to comply with Clean Architecture.
/// Application layer can now depend on this interface without violating architecture rules.
/// </summary>
public interface IPdfImportRepository
{
    Task AddAsync(ImportBatch importBatch, UserId userId);
    Task<IReadOnlyList<ImportBatch>> GetByUserIdAsync(UserId userId);
}
