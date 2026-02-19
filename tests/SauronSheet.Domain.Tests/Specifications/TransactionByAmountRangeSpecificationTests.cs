using Xunit;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Specifications;

/// <summary>
/// Unit tests for TransactionByAmountRangeSpecification.
/// Phase 2 gap fix: this specification was defined in phase-2-spec.md but never implemented.
/// </summary>
public class TransactionByAmountRangeSpecificationTests
{
    private static Transaction CreateTransaction(decimal amount)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(amount, "EUR"),
            DateTime.UtcNow,
            "Test transaction");
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AmountRangeSpec_TransactionWithinRange_ReturnsTrue()
    {
        // Arrange
        var transaction = CreateTransaction(50m);
        var spec = new TransactionByAmountRangeSpecification(
            new Money(10m, "EUR"), new Money(100m, "EUR"));

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AmountRangeSpec_TransactionBelowRange_ReturnsFalse()
    {
        // Arrange
        var transaction = CreateTransaction(5m);
        var spec = new TransactionByAmountRangeSpecification(
            new Money(10m, "EUR"), new Money(100m, "EUR"));

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AmountRangeSpec_TransactionAboveRange_ReturnsFalse()
    {
        // Arrange
        var transaction = CreateTransaction(200m);
        var spec = new TransactionByAmountRangeSpecification(
            new Money(10m, "EUR"), new Money(100m, "EUR"));

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AmountRangeSpec_TransactionAtMinBoundary_ReturnsTrue()
    {
        // Arrange
        var transaction = CreateTransaction(10m);
        var spec = new TransactionByAmountRangeSpecification(
            new Money(10m, "EUR"), new Money(100m, "EUR"));

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AmountRangeSpec_TransactionAtMaxBoundary_ReturnsTrue()
    {
        // Arrange
        var transaction = CreateTransaction(100m);
        var spec = new TransactionByAmountRangeSpecification(
            new Money(10m, "EUR"), new Money(100m, "EUR"));

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void AmountRangeSpec_NegativeAmounts_FiltersCorrectly()
    {
        // Arrange — expenses are negative
        var transaction = CreateTransaction(-50m);
        var spec = new TransactionByAmountRangeSpecification(
            new Money(-100m, "EUR"), new Money(-10m, "EUR"));

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.True(result);
    }
}
