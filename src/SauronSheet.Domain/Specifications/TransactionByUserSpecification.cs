namespace SauronSheet.Domain.Specifications;

using System;
using Entities;
using ValueObjects;

/// <summary>
/// Specification for transactions by user.
/// </summary>
public class TransactionByUserSpecification : BaseSpecification<Transaction>
{
    public TransactionByUserSpecification(UserId userId)
        : base(t => t.UserId == userId)
    {
    }
}
