using Xunit;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Specifications;

/// <summary>
/// Unit tests for TransactionByMultipleImportedFromsSpecification.
/// RF-1a: matches case-insensitively against any value in the list.
/// RF-1b: null ImportedFrom is not matched (null guard).
/// RF-1c: empty source list yields no match.
/// </summary>
public class TransactionByMultipleImportedFromsSpecificationTests
{
    private static Transaction CreateTransaction(string? importedFrom)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(50m, "EUR"),
            DateTime.UtcNow,
            "Test transaction",
            importedFrom: importedFrom);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void MultipleImportedFromsSpec_MatchesOneOfManySources()
    {
        // Arrange
        var transaction = CreateTransaction("nomina.pdf");
        var spec = new TransactionByMultipleImportedFromsSpecification(
            new[] { "facturas.pdf", "nomina.pdf", "otros.pdf" });

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void MultipleImportedFromsSpec_CaseInsensitiveMatch()
    {
        // Arrange — transaction has uppercase, spec has lowercase
        var transaction = CreateTransaction("NOMINA.PDF");
        var spec = new TransactionByMultipleImportedFromsSpecification(
            new[] { "nomina.pdf" });

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void MultipleImportedFromsSpec_NullImportedFrom_DoesNotMatch()
    {
        // Arrange — transaction has no ImportedFrom value
        var transaction = CreateTransaction(null);
        var spec = new TransactionByMultipleImportedFromsSpecification(
            new[] { "nomina.pdf" });

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void MultipleImportedFromsSpec_EmptySourceList_DoesNotMatch()
    {
        // Arrange — empty source list means nothing can match
        var transaction = CreateTransaction("nomina.pdf");
        var spec = new TransactionByMultipleImportedFromsSpecification(
            Array.Empty<string>());

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.False(result);
    }
}
