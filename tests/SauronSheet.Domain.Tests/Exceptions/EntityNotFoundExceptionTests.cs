using SauronSheet.Domain.Exceptions;
using Xunit;

namespace SauronSheet.Domain.Tests.Exceptions;

/// <summary>
/// Unit tests for EntityNotFoundException.
/// </summary>
public class EntityNotFoundExceptionTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void EntityNotFoundException_FormatsMessage()
    {
        var id = Guid.NewGuid();

        var ex = new EntityNotFoundException("Transaction", id);

        Assert.Contains("Transaction", ex.Message);
        Assert.Contains(id.ToString(), ex.Message);
        Assert.Contains("was not found", ex.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void EntityNotFoundException_StoresProperties()
    {
        var id = Guid.NewGuid();

        var ex = new EntityNotFoundException("Category", id);

        Assert.Equal("Category", ex.EntityName);
        Assert.Equal(id, ex.EntityId);
    }
}
