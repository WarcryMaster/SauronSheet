namespace SauronSheet.Domain.Specifications;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Repositories;

/// <summary>
/// Base specification class implementing ISpecification{T}.
/// </summary>
public abstract class BaseSpecification<T> : ISpecification<T> where T : class
{
    public Expression<Func<T, bool>> Criteria { get; protected set; } = null!;
    public int MaxResults { get; protected set; } = 1000;
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();

    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
    }

    protected BaseSpecification()
    {
        Criteria = _ => true; // Default: all items
    }
}

