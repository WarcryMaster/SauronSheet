using SauronSheet.Domain.Common;
using Xunit;

namespace SauronSheet.Domain.Tests.Common;

/// <summary>
/// Unit tests for ValueObject base record.
/// Tests value-based equality provided by C# record semantics.
/// </summary>
public class ValueObjectBaseTests
{
    private record TestValueObject(string Name, int Value) : ValueObject;

    [Fact]
    [Trait("Category", "Domain")]
    public void ValueObject_EqualityByProperties()
    {
        var vo1 = new TestValueObject("test", 42);
        var vo2 = new TestValueObject("test", 42);

        Assert.Equal(vo1, vo2);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ValueObject_InequalityByDifferentProperties()
    {
        var vo1 = new TestValueObject("test", 42);
        var vo2 = new TestValueObject("different", 42);

        Assert.NotEqual(vo1, vo2);
    }
}
