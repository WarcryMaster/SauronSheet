namespace SauronSheet.Domain.Specifications;

using System;
using Entities;

/// <summary>
/// Specification for filtering transactions by imported source file.
/// Matches case-insensitively on t.ImportedFrom.
/// Null guard prevents calling .Equals() on null.
/// </summary>
public class TransactionByImportedFromSpecification : BaseSpecification<Transaction>
{
    public TransactionByImportedFromSpecification(string importedFrom)
        : base(t => t.ImportedFrom != null
                    && t.ImportedFrom.Equals(importedFrom, StringComparison.OrdinalIgnoreCase))
    {
    }
}
