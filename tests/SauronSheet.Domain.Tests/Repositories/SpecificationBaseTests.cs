using SauronSheet.Domain.Repositories;
using Xunit;

namespace SauronSheet.Domain.Tests.Repositories;

/// <summary>
/// Unit tests for ISpecification<T> base interface.
/// </summary>
public class SpecificationBaseTests
{
    private class TestSpecification : ISpecification<object>
    {
        public System.Linq.Expressions.Expression<Func<object, bool>> Criteria =>
            x => true;

        public List<System.Linq.Expressions.Expression<Func<object, object>>> Includes =>
            new();

        public List<string> IncludeStrings =>
            new();
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Specification_DefaultMaxResultsIs1000()
    {
        ISpecification<object> spec = new TestSpecification();

        Assert.Equal(1000, spec.MaxResults);
    }
}
