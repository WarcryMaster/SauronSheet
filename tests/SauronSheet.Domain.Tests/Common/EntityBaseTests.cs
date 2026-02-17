using SauronSheet.Domain.Common;
using Xunit;

namespace SauronSheet.Domain.Tests.Common;

/// <summary>
/// Unit tests for Entity<TId> base class.
/// Tests RED phase (initially failing) until implementation is complete.
/// </summary>
public class EntityBaseTests
{
    // Concrete test implementation for Entity<Guid>
    private class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id)
        {
        }
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Entity_SetsCreatedAtOnConstruction()
    {
        var id = Guid.NewGuid();
        var beforeConstruction = DateTime.UtcNow;

        var entity = new TestEntity(id);

        var afterConstruction = DateTime.UtcNow;

        // CreatedAt should be set to approximately current time
        Assert.True(entity.CreatedAt >= beforeConstruction.AddSeconds(-1));
        Assert.True(entity.CreatedAt <= afterConstruction.AddSeconds(1));
        Assert.Null(entity.UpdatedAt);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Entity_EqualityByIdAndType()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        Assert.Equal(entity1, entity2);
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Entity_InequalityByDifferentId()
    {
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        Assert.NotEqual(entity1, entity2);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Entity_InequalityByDifferentType()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);

        // Create a different entity type with the same ID
        var otherEntity = new OtherTestEntity(id);

        Assert.NotEqual(entity1, (object)otherEntity);
    }

    // Helper class for type comparison test
    private class OtherTestEntity : Entity<Guid>
    {
        public OtherTestEntity(Guid id) : base(id)
        {
        }
    }
}
