namespace Domain.Tests;

using Domain;
using System.Linq.Expressions;
using Xunit;

/// <summary>
/// Tests for IRepository&lt;T&gt; interface
/// </summary>
public class RepositoryTests
{
    private class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }
        public TestEntity() : base() { }
    }

    /// <summary>
    /// T00-004: IRepository&lt;T&gt; interface has 6 methods (Add, Update, Delete, GetById, GetAll, GetBySpec)
    /// </summary>
    [Fact]
    public void IRepository_Should_Have_Six_Methods()
    {
        // Arrange & Act
        var repositoryType = typeof(IRepository<TestEntity>);
        var methods = repositoryType.GetMethods();

        // Assert - should have 6 methods
        var methodNames = methods.Select(m => m.Name).ToList();
        
        Assert.Contains("AddAsync", methodNames);
        Assert.Contains("UpdateAsync", methodNames);
        Assert.Contains("DeleteAsync", methodNames);
        Assert.Contains("GetByIdAsync", methodNames);
        Assert.Contains("GetAllAsync", methodNames);
        Assert.Contains("GetBySpecificationAsync", methodNames);

        // Should have exactly 6 public methods
        Assert.Equal(6, methods.Length);
    }

    [Fact]
    public void IRepository_Methods_Should_Return_Tasks()
    {
        // Arrange
        var repositoryType = typeof(IRepository<TestEntity>);
        var methods = repositoryType.GetMethods();

        // Act & Assert
        foreach (var method in methods)
        {
            Assert.True(
                typeof(System.Threading.Tasks.Task).IsAssignableFrom(method.ReturnType)
                || method.ReturnType.IsGenericType && 
                   method.ReturnType.GetGenericTypeDefinition() == typeof(System.Threading.Tasks.Task<>),
                $"Method {method.Name} should return a Task or Task<T>"
            );
        }
    }
}

/// <summary>
/// Tests for ISpecification&lt;T&gt; interface
/// </summary>
public class SpecificationTests
{
    private class TestEntity : Entity<Guid>
    {
        public string Name { get; set; } = string.Empty;
        
        public TestEntity(Guid id) : base(id) { }
        public TestEntity() : base() { }
    }

    private class TestSpecification : Specification<TestEntity>
    {
        public TestSpecification()
        {
            Criteria = x => x.Name == "Test";
        }
    }

    /// <summary>
    /// T00-005: ISpecification&lt;T&gt; has Criteria property + MaxResults = 1000
    /// </summary>
    [Fact]
    public void ISpecification_Should_Have_Criteria_Property()
    {
        // Arrange
        var spec = new TestSpecification();

        // Act & Assert
        Assert.NotNull(spec.Criteria);
        Assert.Contains("Expression", spec.Criteria.GetType().Name);
    }

    [Fact]
    public void Specification_Should_Have_Default_MaxResults_Of_1000()
    {
        // Arrange
        var spec = new TestSpecification();

        // Act & Assert
        Assert.Equal(1000, spec.MaxResults);
    }

    [Fact]
    public void Specification_Criteria_Should_Be_Evaluable()
    {
        // Arrange
        var spec = new TestSpecification();
        var testEntity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        var compiled = spec.Criteria!.Compile();
        var result = compiled(testEntity);

        // Assert
        Assert.True(result);
    }
}

/// <summary>
/// Tests for IDomainEvent interface
/// </summary>
public class IDomainEventTests
{
    private class TestDomainEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; }

        public TestDomainEvent(DateTime occurredOn)
        {
            OccurredOn = occurredOn;
        }
    }

    /// <summary>
    /// T00-006: IDomainEvent stub interface exists
    /// </summary>
    [Fact]
    public void IDomainEvent_Should_Exist()
    {
        // Arrange
        var interfaceType = typeof(IDomainEvent);

        // Act & Assert
        Assert.NotNull(interfaceType);
        Assert.True(interfaceType.IsInterface);
    }

    [Fact]
    public void IDomainEvent_Should_Have_OccurredOn_Property()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var domainEvent = new TestDomainEvent(now);

        // Act & Assert
        Assert.Equal(now, domainEvent.OccurredOn);
    }
}
