using Xunit;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Domain.Tests.Specifications;

/// <summary>
/// Unit tests for TransactionByDescriptionKeywordSpecification.
/// Phase 4: Keyword-based filtering for transaction search.
/// </summary>
public class TransactionByDescriptionKeywordSpecificationTests
{
    private static Transaction CreateTransaction(string description)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(50m, "EUR"),
            DateTime.UtcNow,
            description);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DescriptionKeywordSpec_MatchesPartialKeyword()
    {
        // Arrange
        var transaction = CreateTransaction("Morning Coffee at Starbucks");
        var spec = new TransactionByDescriptionKeywordSpecification("coffee");

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DescriptionKeywordSpec_NoMatch_ReturnsFalse()
    {
        // Arrange
        var transaction = CreateTransaction("Grocery shopping");
        var spec = new TransactionByDescriptionKeywordSpecification("coffee");

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DescriptionKeywordSpec_EmptyKeyword_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            new TransactionByDescriptionKeywordSpecification(""));
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void DescriptionKeywordSpec_CaseInsensitive()
    {
        // Arrange
        var transaction = CreateTransaction("COFFEE BEANS");
        var spec = new TransactionByDescriptionKeywordSpecification("coffee");

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.True(result);
    }
}
