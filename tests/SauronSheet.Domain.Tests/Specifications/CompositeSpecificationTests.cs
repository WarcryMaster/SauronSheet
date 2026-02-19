using Xunit;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Specifications;

/// <summary>
/// Unit tests for CompositeSpecification&lt;T&gt;.
/// Phase 4: Composable specification pattern for multi-filter queries.
/// </summary>
public class CompositeSpecificationTests
{
    private static Transaction CreateTransaction(
        string userId, Guid? categoryId, DateTime date, string description = "Test")
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId(userId),
            new Money(100m, "EUR"),
            date,
            description,
            categoryId.HasValue ? new CategoryId(categoryId.Value) : null);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CompositeSpec_And_CombinesTwoSpecs()
    {
        // Arrange
        var userId = "user-1";
        var catId = Guid.NewGuid();
        var transaction = CreateTransaction(userId, catId, DateTime.UtcNow);

        var userSpec = new TransactionByUserSpecification(new UserId(userId));
        var catSpec = new TransactionByCategorySpecification(new CategoryId(catId));
        var composite = CompositeSpecification<Transaction>.And(userSpec, catSpec);

        // Act
        var result = composite.Criteria.Compile()(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CompositeSpec_And_RejectsMismatch()
    {
        // Arrange
        var catId = Guid.NewGuid();
        var otherCatId = Guid.NewGuid();
        var transaction = CreateTransaction("user-1", catId, DateTime.UtcNow);

        var userSpec = new TransactionByUserSpecification(new UserId("user-1"));
        var catSpec = new TransactionByCategorySpecification(new CategoryId(otherCatId));
        var composite = CompositeSpecification<Transaction>.And(userSpec, catSpec);

        // Act
        var result = composite.Criteria.Compile()(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void CompositeSpec_And_MultipleSpecs()
    {
        // Arrange
        var userId = "user-1";
        var catId = Guid.NewGuid();
        var date = new DateTime(2026, 1, 15);
        var transaction = CreateTransaction(userId, catId, date);

        var userSpec = new TransactionByUserSpecification(new UserId(userId));
        var dateSpec = new TransactionByDateRangeSpecification(
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)); // Transaction is in January, range is February
        var catSpec = new TransactionByCategorySpecification(new CategoryId(catId));

        var composite = CompositeSpecification<Transaction>.And(
            CompositeSpecification<Transaction>.And(userSpec, dateSpec),
            catSpec);

        // Act
        var result = composite.Criteria.Compile()(transaction);

        // Assert
        Assert.False(result); // Date doesn't match
    }
}
