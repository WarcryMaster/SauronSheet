using Xunit;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Specifications;

/// <summary>
/// Unit tests for TransactionByImportedFromSpecification.
/// RF-1a: case-insensitive match.
/// RF-1b: null ImportedFrom not matched.
/// </summary>
public class TransactionByImportedFromSpecificationTests
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
    public void ImportedFromSpec_MatchesExactValue()
    {
        // Arrange
        var transaction = CreateTransaction("nomina.pdf");
        var spec = new TransactionByImportedFromSpecification("nomina.pdf");

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ImportedFromSpec_CaseInsensitiveMatch()
    {
        // Arrange - different casing
        var transaction = CreateTransaction("NOMINA.PDF");
        var spec = new TransactionByImportedFromSpecification("nomina.pdf");

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ImportedFromSpec_NullImportedFrom_DoesNotMatch()
    {
        // Arrange - transaction with null ImportedFrom
        var transaction = CreateTransaction(null);
        var spec = new TransactionByImportedFromSpecification("nomina.pdf");

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void ImportedFromSpec_DifferentValue_DoesNotMatch()
    {
        // Arrange
        var transaction = CreateTransaction("facturas.pdf");
        var spec = new TransactionByImportedFromSpecification("nomina.pdf");

        // Act
        var result = spec.Criteria.Compile()(transaction);

        // Assert
        Assert.False(result);
    }
}
