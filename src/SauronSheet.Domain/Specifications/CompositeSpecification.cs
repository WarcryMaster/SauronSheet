namespace SauronSheet.Domain.Specifications;

using System;
using System.Linq.Expressions;
using Repositories;

/// <summary>
/// Composable specification that combines two specifications with AND logic.
/// Phase 4: Required for composing multiple filters in analytics queries.
/// </summary>
public class CompositeSpecification<T> : BaseSpecification<T> where T : class
{
    private CompositeSpecification(Expression<Func<T, bool>> criteria)
        : base(criteria)
    {
    }

    /// <summary>
    /// Combines two specifications using AND logic.
    /// </summary>
    public static CompositeSpecification<T> And(ISpecification<T> left, ISpecification<T> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var combined = Expression.AndAlso(
            Expression.Invoke(left.Criteria, parameter),
            Expression.Invoke(right.Criteria, parameter));
        var lambda = Expression.Lambda<Func<T, bool>>(combined, parameter);

        return new CompositeSpecification<T>(lambda);
    }
}
