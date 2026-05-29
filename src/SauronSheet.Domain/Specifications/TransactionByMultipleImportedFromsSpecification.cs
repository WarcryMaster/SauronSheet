namespace SauronSheet.Domain.Specifications;

using System;
using System.Collections.Generic;
using System.Linq;
using Entities;

/// <summary>
/// Specification for filtering transactions by multiple imported source files.
/// Matches case-insensitively on t.ImportedFrom against any value in the provided list.
/// Null guard prevents calling .Equals() on null.
/// </summary>
public class TransactionByMultipleImportedFromsSpecification : BaseSpecification<Transaction>
{
    public TransactionByMultipleImportedFromsSpecification(IEnumerable<string> importedFroms)
        : base(t => t.ImportedFrom != null
                    && importedFroms.Any(source =>
                        t.ImportedFrom.Equals(source, StringComparison.OrdinalIgnoreCase)))
    {
    }
}
