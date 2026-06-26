namespace SauronSheet.Domain.Specifications;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Entities;
using Exceptions;
using ValueObjects;

/// <summary>
/// Specification for filtering transactions by multiple IDs with multi-tenant safety.
/// Used for bulk delete operations to ensure atomic, safe deletion.
/// Enforces:
/// - UserId scoping (multi-tenant isolation)
/// - MaxResults limit (1000 transactions per operation)
/// - Atomic consistency guarantees
/// </summary>
public class TransactionByIdSpecification : BaseSpecification<Transaction>
{
    /// <summary>
    /// Creates a specification for filtering transactions by ID list and user.
    /// </summary>
    /// <param name="userId">The user requesting the deletion (tenant scoping required)</param>
    /// <param name="transactionIds">Collection of transaction IDs to filter</param>
    /// <exception cref="DomainException">Thrown if UserId is null, empty IDs, or >1000 IDs</exception>
    public TransactionByIdSpecification(UserId userId, IEnumerable<TransactionId> transactionIds)
        : base(BuildCriteria(userId, transactionIds))
    {
        // MaxResults is inherited from BaseSpecification as 1000
        // Validate during construction
        if (userId == null)
            throw new DomainException("UserId cannot be null for bulk delete specification.");

        List<TransactionId> idList = transactionIds?.ToList() ?? new List<TransactionId>();

        if (idList.Count > MaxResults)
            throw new DomainException($"Cannot delete more than {MaxResults} transactions in a single operation. Requested: {idList.Count}");

        if (idList.Count == 0)
            throw new DomainException("At least one transaction ID must be provided for deletion.");
    }

    /// <summary>
    /// Builds the filtering criteria: WHERE user_id = @userId AND id IN (@ids)
    /// </summary>
    private static Expression<Func<Transaction, bool>> BuildCriteria(UserId userId, IEnumerable<TransactionId> transactionIds)
    {
        if (userId == null)
            throw new DomainException("UserId cannot be null.");

        List<TransactionId> idList = transactionIds?.ToList() ?? new List<TransactionId>();

        if (idList.Count == 0)
            throw new DomainException("At least one transaction ID must be provided.");

        if (idList.Count > 1000)
            throw new DomainException("Cannot filter more than 1000 transactions.");

        // Capture idList in closure for use in expression
        List<TransactionId> ids = idList;
        
        // Return compiled criteria: transaction belongs to user AND ID is in the requested list
        return transaction => transaction.UserId == userId && ids.Contains(transaction.Id);
    }
}
