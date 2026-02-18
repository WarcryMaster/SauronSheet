namespace SauronSheet.Domain.Specifications;

using System;
using Entities;
using ValueObjects;

/// <summary>
/// Specification for transactions by category.
/// </summary>
public class TransactionByCategorySpecification : BaseSpecification<Transaction>
{
    public TransactionByCategorySpecification(CategoryId categoryId)
        : base(t => t.CategoryId == categoryId)
    {
    }
}
