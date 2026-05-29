using SauronSheet.Domain.Repositories;
using Xunit;

namespace SauronSheet.Domain.Tests.Records;

/// <summary>
/// Unit tests for the BankCategoryTranslation record.
/// Verifies construction, value equality, and with-expression immutability.
/// </summary>
public class BankCategoryTranslationTests
{
    [Fact]
    [Trait("Category", "Domain")]
    public void BankCategoryTranslation_Construction_SetsAllProperties()
    {
        // Arrange & Act
        var record = new BankCategoryTranslation(
            BankCategory: "ALIMENTACION",
            BankSubcategory: "SUPERMERCADOS",
            ResolvedCategoryName: "Food",
            ResolvedSubcategoryName: "Groceries");

        // Assert
        Assert.Equal("ALIMENTACION", record.BankCategory);
        Assert.Equal("SUPERMERCADOS", record.BankSubcategory);
        Assert.Equal("Food", record.ResolvedCategoryName);
        Assert.Equal("Groceries", record.ResolvedSubcategoryName);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void BankCategoryTranslation_TwoIdenticalInstances_AreValueEqual()
    {
        // Arrange
        var first = new BankCategoryTranslation("ALIMENTACION", null, "Food", null);
        var second = new BankCategoryTranslation("ALIMENTACION", null, "Food", null);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void BankCategoryTranslation_WithExpression_MutatesTargetProperty()
    {
        // Arrange
        var original = new BankCategoryTranslation("ALIMENTACION", null, "Food", null);

        // Act
        var mutated = original with { ResolvedCategoryName = "Groceries" };

        // Assert
        Assert.Equal("Groceries", mutated.ResolvedCategoryName);
        Assert.Equal("ALIMENTACION", mutated.BankCategory);
        Assert.Equal("Food", original.ResolvedCategoryName); // original is unchanged
    }
}
