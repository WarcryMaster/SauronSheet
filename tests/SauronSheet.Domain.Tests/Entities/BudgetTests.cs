using Xunit;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Entities;

public class BudgetTests
{
    // ─────────────────────────────────────────────
    // Construction (Task 1.2)
    // ─────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_ValidConstruction_SetsAllProperties()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var effectiveFrom = new DateOnly(2026, 1, 1);
        var limit = new Money(500m, "EUR");

        // Act
        var budget = new Budget(budgetId, userId, categoryId,
            effectiveFrom, effectiveUntil: null, BudgetPeriod.Monthly, limit);

        // Assert
        Assert.Equal(budgetId, budget.Id);
        Assert.Equal(userId, budget.UserId);
        Assert.Equal(categoryId, budget.CategoryId);
        Assert.Equal(effectiveFrom, budget.EffectiveFrom);
        Assert.Null(budget.EffectiveUntil);
        Assert.Equal(BudgetPeriod.Monthly, budget.PeriodGranularity);
        Assert.Equal(limit, budget.Limit);
        Assert.True(budget.CreatedAt <= DateTime.UtcNow);
        Assert.Null(budget.UpdatedAt);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_WithEffectiveUntil_SetsBothDates()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var effectiveFrom = new DateOnly(2026, 1, 1);
        var effectiveUntil = new DateOnly(2026, 12, 31);
        var limit = new Money(500m, "EUR");

        // Act
        var budget = new Budget(budgetId, userId, categoryId,
            effectiveFrom, effectiveUntil, BudgetPeriod.Monthly, limit);

        // Assert
        Assert.Equal(effectiveFrom, budget.EffectiveFrom);
        Assert.Equal(effectiveUntil, budget.EffectiveUntil);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_QuarterlyGranularity_SetsCorrectly()
    {
        // Arrange & Act
        var budget = CreateValidBudget(
            granularity: BudgetPeriod.Quarterly,
            effectiveFrom: new DateOnly(2026, 1, 1));

        // Assert
        Assert.Equal(BudgetPeriod.Quarterly, budget.PeriodGranularity);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_ZeroLimit_ThrowsDomainException()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var effectiveFrom = new DateOnly(2026, 1, 1);
        var limit = new Money(0m, "EUR");

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Budget(budgetId, userId, categoryId,
                effectiveFrom, effectiveUntil: null, BudgetPeriod.Monthly, limit));
        Assert.Contains("positive", ex.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_NegativeLimit_ThrowsDomainException()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var effectiveFrom = new DateOnly(2026, 1, 1);
        var limit = new Money(-100m, "EUR");

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Budget(budgetId, userId, categoryId,
                effectiveFrom, effectiveUntil: null, BudgetPeriod.Monthly, limit));
        Assert.Contains("positive", ex.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_EffectiveUntil_Before_EffectiveFrom_ThrowsDomainException()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var effectiveFrom = new DateOnly(2026, 6, 1);
        var effectiveUntil = new DateOnly(2026, 1, 1); // before EffectiveFrom
        var limit = new Money(500m, "EUR");

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Budget(budgetId, userId, categoryId,
                effectiveFrom, effectiveUntil, BudgetPeriod.Monthly, limit));
        Assert.Contains("EffectiveUntil", ex.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Budget_EffectiveUntil_Equals_EffectiveFrom_Valid()
    {
        // Arrange
        var budgetId = new BudgetId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var sameDate = new DateOnly(2026, 1, 1);
        var limit = new Money(500m, "EUR");

        // Act — should not throw
        var budget = new Budget(budgetId, userId, categoryId,
            sameDate, sameDate, BudgetPeriod.Monthly, limit);

        // Assert
        Assert.Equal(sameDate, budget.EffectiveFrom);
        Assert.Equal(sameDate, budget.EffectiveUntil);
    }

    // ─────────────────────────────────────────────
    // UpdateLimit (Task 1.3)
    // ─────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdateLimit_ValidPositiveAmount_UpdatesLimitAndTimestamp()
    {
        // Arrange
        var budget = CreateValidBudget(limitAmount: 500m);

        // Act
        budget.UpdateLimit(new Money(750m, "EUR"));

        // Assert
        Assert.Equal(750m, budget.Limit.Amount);
        Assert.NotNull(budget.UpdatedAt);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdateLimit_ZeroLimit_ThrowsDomainException()
    {
        // Arrange
        var budget = CreateValidBudget(limitAmount: 500m);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            budget.UpdateLimit(new Money(0m, "EUR")));
        Assert.Contains("positive", ex.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdateLimit_NegativeLimit_ThrowsDomainException()
    {
        // Arrange
        var budget = CreateValidBudget(limitAmount: 500m);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            budget.UpdateLimit(new Money(-50m, "EUR")));
        Assert.Contains("positive", ex.Message);
    }

    // ─────────────────────────────────────────────
    // UpdateEffectiveDates (Task 1.3)
    // ─────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdateEffectiveDates_ValidDates_UpdatesBothDates()
    {
        // Arrange
        var budget = CreateValidBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: null);

        // Act
        budget.UpdateEffectiveDates(
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 12, 31));

        // Assert
        Assert.Equal(new DateOnly(2026, 6, 1), budget.EffectiveFrom);
        Assert.Equal(new DateOnly(2026, 12, 31), budget.EffectiveUntil);
        Assert.NotNull(budget.UpdatedAt);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdateEffectiveDates_UntilBeforeFrom_ThrowsDomainException()
    {
        // Arrange
        var budget = CreateValidBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: null);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            budget.UpdateEffectiveDates(
                new DateOnly(2026, 12, 1),
                new DateOnly(2026, 1, 1)));
        Assert.Contains("EffectiveUntil", ex.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdateEffectiveDates_ClearUntil_MakesPermanent()
    {
        // Arrange
        var budget = CreateValidBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 12, 31));

        // Act
        budget.UpdateEffectiveDates(
            new DateOnly(2026, 1, 1),
            until: null);

        // Assert
        Assert.Null(budget.EffectiveUntil);
    }

    // ─────────────────────────────────────────────
    // UpdateGranularity (Task 1.3)
    // ─────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdateGranularity_ChangesPeriodGranularity()
    {
        // Arrange
        var budget = CreateValidBudget(granularity: BudgetPeriod.Monthly);

        // Act
        budget.UpdateGranularity(BudgetPeriod.Annual);

        // Assert
        Assert.Equal(BudgetPeriod.Annual, budget.PeriodGranularity);
        Assert.NotNull(budget.UpdatedAt);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void UpdateGranularity_QuarterlyToSemester_ChangesCorrectly()
    {
        // Arrange
        var budget = CreateValidBudget(granularity: BudgetPeriod.Quarterly);

        // Act
        budget.UpdateGranularity(BudgetPeriod.Semester);

        // Assert
        Assert.Equal(BudgetPeriod.Semester, budget.PeriodGranularity);
    }

    // ─────────────────────────────────────────────
    // Deactivate (Task 1.3)
    // ─────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Domain")]
    public void Deactivate_SetsEffectiveUntilToGivenDate()
    {
        // Arrange
        var budget = CreateValidBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: null);
        var deactivationDate = new DateOnly(2026, 5, 30);

        // Act
        budget.Deactivate(deactivationDate);

        // Assert
        Assert.Equal(deactivationDate, budget.EffectiveUntil);
        Assert.NotNull(budget.UpdatedAt);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Deactivate_AlreadyActiveWithEndDate_OverwritesEffectiveUntil()
    {
        // Arrange
        var budget = CreateValidBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 12, 31));
        var earlierDate = new DateOnly(2026, 6, 30);

        // Act
        budget.Deactivate(earlierDate);

        // Assert
        Assert.Equal(earlierDate, budget.EffectiveUntil);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Deactivate_BeforeEffectiveFrom_ThrowsDomainException()
    {
        // Arrange
        var budget = CreateValidBudget(
            effectiveFrom: new DateOnly(2026, 6, 1),
            effectiveUntil: null);
        var tooEarly = new DateOnly(2026, 1, 1);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            budget.Deactivate(tooEarly));
        Assert.Contains("EffectiveFrom", ex.Message);
    }

    // ─────────────────────────────────────────────
    // IsActiveOn (Task 1.3)
    // ─────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Domain")]
    public void IsActiveOn_DateWithinRange_ReturnsTrue()
    {
        // Arrange
        var budget = CreateValidBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 12, 31));

        // Act
        var result = budget.IsActiveOn(new DateOnly(2026, 6, 15));

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void IsActiveOn_DateBeforeEffectiveFrom_ReturnsFalse()
    {
        // Arrange
        var budget = CreateValidBudget(
            effectiveFrom: new DateOnly(2026, 6, 1),
            effectiveUntil: null);

        // Act
        var result = budget.IsActiveOn(new DateOnly(2026, 1, 1));

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void IsActiveOn_DateAfterEffectiveUntil_ReturnsFalse()
    {
        // Arrange
        var budget = CreateValidBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 6, 30));

        // Act
        var result = budget.IsActiveOn(new DateOnly(2026, 7, 1));

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void IsActiveOn_DateEqualsEffectiveFrom_ReturnsTrue()
    {
        // Arrange
        var budget = CreateValidBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: null);

        // Act
        var result = budget.IsActiveOn(new DateOnly(2026, 1, 1));

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void IsActiveOn_DateEqualsEffectiveUntil_ReturnsTrue()
    {
        // Arrange
        var budget = CreateValidBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 6, 30));

        // Act
        var result = budget.IsActiveOn(new DateOnly(2026, 6, 30));

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void IsActiveOn_PermanentBudget_AlwaysActiveAfterEffectiveFrom()
    {
        // Arrange
        var budget = CreateValidBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: null);

        // Act
        var result = budget.IsActiveOn(new DateOnly(2099, 12, 31));

        // Assert
        Assert.True(result);
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private static Budget CreateValidBudget(
        decimal limitAmount = 500m,
        BudgetPeriod granularity = BudgetPeriod.Monthly,
        DateOnly? effectiveFrom = null,
        DateOnly? effectiveUntil = null)
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId("user-1"),
            new CategoryId(Guid.NewGuid()),
            effectiveFrom ?? new DateOnly(2026, 1, 1),
            effectiveUntil,
            granularity,
            new Money(limitAmount, "EUR"));
    }
}
