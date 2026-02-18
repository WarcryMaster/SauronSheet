namespace SauronSheet.Domain.Entities;

using Common;
using Exceptions;

/// <summary>
/// Represents metadata about a PDF import batch.
/// NOTE: This is an Entity (not a Value Object) because it has database identity and lifecycle.
/// </summary>
public class ImportBatch : Entity<Guid>
{
    public string Filename { get; private set; }
    public int ImportedCount { get; private set; }
    public int SkippedCount { get; private set; }
    public DateTime ImportedAt { get; private set; }

    /// <summary>
    /// Total processed rows (CRITICAL FIX I-4: calculated property)
    /// </summary>
    public int TotalProcessed => ImportedCount + SkippedCount;

    public ImportBatch(
        Guid id,
        string filename,
        int importedCount,
        int skippedCount,
        DateTime importedAt)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(filename))
            throw new DomainException("Filename is required.");
        
        if (importedCount < 0)
            throw new DomainException("Imported count cannot be negative.");
        
        if (skippedCount < 0)
            throw new DomainException("Skipped count cannot be negative.");

        Filename = filename;
        ImportedCount = importedCount;
        SkippedCount = skippedCount;
        ImportedAt = importedAt;
    }

    public override string ToString()
        => $"{Filename}: {ImportedCount} imported, {SkippedCount} skipped at {ImportedAt:yyyy-MM-dd HH:mm}";
}
