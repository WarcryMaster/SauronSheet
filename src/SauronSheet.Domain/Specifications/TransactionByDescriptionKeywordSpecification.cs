namespace SauronSheet.Domain.Specifications;

using Entities;
using Exceptions;

/// <summary>
/// Specification for transactions matching a description keyword (case-insensitive partial match).
/// Phase 4: Required for transaction search & filtering.
/// </summary>
public class TransactionByDescriptionKeywordSpecification : BaseSpecification<Transaction>
{
    public TransactionByDescriptionKeywordSpecification(string keyword)
        : base(t => t.Description.ToLower().Contains(keyword.ToLower()))
    {
        if (string.IsNullOrWhiteSpace(keyword))
            throw new DomainException("Search keyword cannot be empty.");
    }
}
