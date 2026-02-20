using Xunit;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Entities;

public class BudgetTests
{
    // === Construction Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_ValidConstruction_SetsAllProperties()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var period = new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));
        var limit = new Money(500m, "EUR");

        // Act
        var budget = new Budget(budgetId, userId, categoryId, period, limit);

        // Assert
        Assert.Equal(budgetId, budget.Id);
        Assert.Equal(userId, budget.UserId);
        Assert.Equal(categoryId, budget.CategoryId);
        Assert.Equal(period, budget.Period);
        Assert.Equal(limit, budget.Limit);
        Assert.True(budget.CreatedAt <= DateTime.UtcNow);
        Assert.Null(budget.UpdatedAt);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_NullUserId_ThrowsArgumentNullException()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var categoryId = new CategoryId(Guid.NewGuid());
        var period = new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));
        var limit = new Money(500m, "EUR");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Budget(budgetId, null!, categoryId, period, limit));
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_NullCategoryId_ThrowsArgumentNullException()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var period = new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));
        var limit = new Money(500m, "EUR");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Budget(budgetId, userId, null!, period, limit));
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_NullPeriod_ThrowsArgumentNullException()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var limit = new Money(500m, "EUR");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Budget(budgetId, userId, categoryId, null!, limit));
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_NullLimit_ThrowsArgumentNullException()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var period = new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Budget(budgetId, userId, categoryId, period, null!));
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_ZeroLimit_ThrowsDomainException()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var period = new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));
        var limit = new Money(0m, "EUR");

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Budget(budgetId, userId, categoryId, period, limit));
        Assert.Contains("Budget limit must be positive", ex.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_NegativeLimit_ThrowsDomainException()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var period = new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));
        var limit = new Money(-100m, "EUR");

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Budget(budgetId, userId, categoryId, period, limit));
        Assert.Contains("Budget limit must be positive", ex.Message);
    }

    // === IsOverBudget Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void IsOverBudget_SpendExceedsLimit_ReturnsTrue()
    {
        // Arrange
        var budget = CreateValidBudget(500m);
        var currentSpend = new Money(600m, "EUR");

        // Act
        var result = budget.IsOverBudget(currentSpend);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void IsOverBudget_SpendBelowLimit_ReturnsFalse()
    {
        // Arrange
        var budget = CreateValidBudget(500m);
        var currentSpend = new Money(300m, "EUR");

        // Act
        var result = budget.IsOverBudget(currentSpend);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void IsOverBudget_SpendEqualsLimit_ReturnsFalse()
    {
        // Arrange
        var budget = CreateValidBudget(500m);
        var currentSpend = new Money(500m, "EUR");

        // Act
        var result = budget.IsOverBudget(currentSpend);

        // Assert
        Assert.False(result);
    }

    // === PercentageUsed Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void PercentageUsed_ZeroSpend_ReturnsZero()
    {
        // Arrange
        var budget = CreateValidBudget(500m);
        var currentSpend = new Money(0m, "EUR");

        // Act
        var result = budget.PercentageUsed(currentSpend);

        // Assert
        Assert.Equal(0.0m, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PercentageUsed_HalfSpend_ReturnsFiftyPercent()
    {
        // Arrange
        var budget = CreateValidBudget(500m);
        var currentSpend = new Money(250m, "EUR");

        // Act
        var result = budget.PercentageUsed(currentSpend);

        // Assert
        Assert.Equal(0.50m, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PercentageUsed_OverSpend_ReturnsGreaterThanOne()
    {
        // Arrange
        var budget = CreateValidBudget(500m);
        var currentSpend = new Money(625m, "EUR");

        // Act
        var result = budget.PercentageUsed(currentSpend);

        // Assert
        Assert.Equal(1.25m, result);
    }

    // === RemainingAmount Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void RemainingAmount_UnderBudget_ReturnsPositive()
    {
        // Arrange
        var budget = CreateValidBudget(500m);
        var currentSpend = new Money(300m, "EUR");

        // Act
        var result = budget.RemainingAmount(currentSpend);

        // Assert
        Assert.Equal(200m, result.Amount);
        Assert.Equal("EUR", result.Currency);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void RemainingAmount_OverBudget_ReturnsNegative()
    {
        // Arrange
        var budget = CreateValidBudget(500m);
        var currentSpend = new Money(700m, "EUR");

        // Act
        var result = budget.RemainingAmount(currentSpend);

        // Assert
        Assert.Equal(-200m, result.Amount);
        Assert.Equal("EUR", result.Currency);
    }

    // === UpdateLimit Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdateLimit_ValidPositiveLimit_UpdatesLimitAndTimestamp()
    {
        // Arrange
        var budget = CreateValidBudget(500m);

        // Act
        budget.UpdateLimit(new Money(600m, "EUR"));

        // Assert
        Assert.Equal(600m, budget.Limit.Amount);
        Assert.NotNull(budget.UpdatedAt);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdateLimit_ZeroLimit_ThrowsDomainException()
    {
        // Arrange
        var budget = CreateValidBudget(500m);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            budget.UpdateLimit(new Money(0m, "EUR")));
        Assert.Contains("Budget limit must be positive", ex.Message);
    }

    // === Currency Validation Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void IsOverBudget_CurrencyMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var budget = CreateValidBudget(500m);
        var currentSpend = new Money(300m, "USD");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            budget.IsOverBudget(currentSpend));
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PercentageUsed_CurrencyMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var budget = CreateValidBudget(500m);
        var currentSpend = new Money(250m, "USD");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            budget.PercentageUsed(currentSpend));
    }

    // === Helpers ===

    private static Budget CreateValidBudget(decimal limitAmount)
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId("user-1"),
            new CategoryId(Guid.NewGuid()),
            new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)),
            new Money(limitAmount, "EUR"));
    }
}
