namespace Domain.Tests;

using Domain;
using Xunit;

/// <summary>
/// Tests for Entity&lt;TId&gt; base class
/// </summary>
public class EntityTests
{
    // Test entity for testing purposes
    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id)
        {
        }

        public TestEntity() : base()
        {
        }
    }

    /// <summary>
    /// T00-001: Entity&lt;TId&gt; base class has ID property (type: Guid)
    /// </summary>
    [Fact]
    public void Entity_Should_Have_Guid_Id()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new TestEntity(id);

        // Act & Assert
        Assert.Equal(id, entity.Id);
        Assert.IsType<Guid>(entity.Id);
    }

    /// <summary>
    /// T00-001 Extended: Entity should have CreatedAt and UpdatedAt
    /// </summary>
    [Fact]
    public void Entity_Should_Have_Timestamps()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid() };

        // Act & Assert
        Assert.NotEqual(default, entity.CreatedAt);
        Assert.Null(entity.UpdatedAt);
    }
}

/// <summary>
/// Tests for ValueObject base class
/// </summary>
public class ValueObjectTests
{
    // Test value object for testing purposes
    private class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    /// <summary>
    /// T00-002: ValueObject implements value-based equality
    /// </summary>
    [Fact]
    public void ValueObject_Should_Implement_Value_Based_Equality()
    {
        // Arrange
        var money1 = new Money(10m, "USD");
        var money2 = new Money(10m, "USD");
        var money3 = new Money(20m, "USD");

        // Act & Assert
        Assert.Equal(money1, money2);
        Assert.NotEqual(money1, money3);
    }

    [Fact]
    public void ValueObject_Should_Have_Same_Hash_When_Equal()
    {
        // Arrange
        var money1 = new Money(10m, "USD");
        var money2 = new Money(10m, "USD");

        // Act & Assert
        Assert.Equal(money1.GetHashCode(), money2.GetHashCode());
    }
}

/// <summary>
/// Tests for domain exceptions
/// </summary>
public class ExceptionTests
{
    /// <summary>
    /// T00-003: DomainException + EntityNotFoundException + ValueObjectValidationException inherit correctly
    /// </summary>
    [Fact]
    public void DomainException_Should_Inherit_From_Exception()
    {
        // Arrange & Act
        var ex = new DomainException("Test message");

        // Assert
        Assert.IsType<DomainException>(ex);
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("Test message", ex.Message);
    }

    [Fact]
    public void EntityNotFoundException_Should_Inherit_From_DomainException()
    {
        // Arrange & Act
        var ex = new EntityNotFoundException("Category", Guid.NewGuid());

        // Assert
        Assert.IsType<EntityNotFoundException>(ex);
        Assert.IsAssignableFrom<DomainException>(ex);
        Assert.Contains("Category", ex.Message);
    }

    [Fact]
    public void ValueObjectValidationException_Should_Inherit_From_DomainException()
    {
        // Arrange & Act
        var ex = new ValueObjectValidationException("Invalid value");

        // Assert
        Assert.IsType<ValueObjectValidationException>(ex);
        Assert.IsAssignableFrom<DomainException>(ex);
    }
}
