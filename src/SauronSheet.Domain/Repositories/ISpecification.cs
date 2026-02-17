using System.Linq.Expressions;

namespace SauronSheet.Domain.Repositories;

/// <summary>
/// Base interface for specifications (filtering contracts).
/// Encapsulates filtering logic in domain language.
/// MaxResults defaults to 1000 to prevent runaway queries.
/// Pagination required for larger datasets.
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    int MaxResults => 1000;
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
}
