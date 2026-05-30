using Xunit;
using Moq;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Services;

public class BudgetServiceTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();

    // ─────────────────────────────────────────────
    // ValidateNoOverlap (Task 1.5)
    // ─────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateNoOverlap_NoExistingBudgets_Succeeds()
    {
        // Arrange
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(new List<Budget>());

        var service = new BudgetService(_budgetRepoMock.Object);

        // Act & Assert — no exception thrown
        await service.ValidateNoOverlap(
            userId, categoryId,
            from: new DateOnly(2026, 1, 1),
            until: null);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateNoOverlap_ExistingPermanentBudget_NewOverlaps_Throws()
    {
        // Arrange
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());

        var permanentBudget = new Budget(
            new BudgetId(Guid.NewGuid()),
            userId,
            categoryId,
            new DateOnly(2026, 1, 1),
            effectiveUntil: null,
            BudgetPeriod.Monthly,
            new Money(500m, "EUR"));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(new List<Budget> { permanentBudget });

        var service = new BudgetService(_budgetRepoMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.ValidateNoOverlap(
                userId, categoryId,
                from: new DateOnly(2026, 6, 1),
                until: null));

        Assert.Contains("overlap", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateNoOverlap_AdjacentRanges_NoOverlap_Succeeds()
    {
        // Arrange
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());

        var existingBudget = new Budget(
            new BudgetId(Guid.NewGuid()),
            userId,
            categoryId,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 6, 30),
            BudgetPeriod.Monthly,
            new Money(500m, "EUR"));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(new List<Budget> { existingBudget });

        var service = new BudgetService(_budgetRepoMock.Object);

        // Act & Assert — no exception: existing ends 2026-06-30, new starts 2026-07-01
        await service.ValidateNoOverlap(
            userId, categoryId,
            from: new DateOnly(2026, 7, 1),
            until: null);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateNoOverlap_ExistingEndsOnSameDay_NewStartsNextDay_Succeeds()
    {
        // Arrange
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());

        var existingBudget = new Budget(
            new BudgetId(Guid.NewGuid()),
            userId,
            categoryId,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 6, 30),
            BudgetPeriod.Monthly,
            new Money(500m, "EUR"));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(new List<Budget> { existingBudget });

        var service = new BudgetService(_budgetRepoMock.Object);

        // Act & Assert — adjacent: starts at existing end + 1 day
        await service.ValidateNoOverlap(
            userId, categoryId,
            from: new DateOnly(2026, 7, 1),
            until: new DateOnly(2026, 12, 31));
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateNoOverlap_OverlappingRanges_Throws()
    {
        // Arrange
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());

        var existingBudget = new Budget(
            new BudgetId(Guid.NewGuid()),
            userId,
            categoryId,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 9, 30),
            BudgetPeriod.Monthly,
            new Money(500m, "EUR"));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(new List<Budget> { existingBudget });

        var service = new BudgetService(_budgetRepoMock.Object);

        // Act & Assert — new budget [2026-06-01, 2026-12-31] overlaps with [2026-03-01, 2026-09-30]
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.ValidateNoOverlap(
                userId, categoryId,
                from: new DateOnly(2026, 6, 1),
                until: new DateOnly(2026, 12, 31)));

        Assert.Contains("overlap", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateNoOverlap_NewIsPermanent_ExistingHasEnd_NoOverlap_Succeeds()
    {
        // Arrange
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());

        var existingBudget = new Budget(
            new BudgetId(Guid.NewGuid()),
            userId,
            categoryId,
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 12, 31),
            BudgetPeriod.Monthly,
            new Money(500m, "EUR"));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(new List<Budget> { existingBudget });

        var service = new BudgetService(_budgetRepoMock.Object);

        // Act & Assert — existing ended in 2025, new starts 2026
        await service.ValidateNoOverlap(
            userId, categoryId,
            from: new DateOnly(2026, 1, 1),
            until: null);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateNoOverlap_ExcludeBudgetId_IgnoresExcluded()
    {
        // Arrange
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var budgetToExclude = new BudgetId(Guid.NewGuid());

        var existingBudget = new Budget(
            budgetToExclude,
            userId,
            categoryId,
            new DateOnly(2026, 1, 1),
            effectiveUntil: null,
            BudgetPeriod.Monthly,
            new Money(500m, "EUR"));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(new List<Budget> { existingBudget });

        var service = new BudgetService(_budgetRepoMock.Object);

        // Act & Assert — the only existing budget is excluded; no overlap
        await service.ValidateNoOverlap(
            userId, categoryId,
            from: new DateOnly(2026, 1, 1),
            until: null,
            excludeBudgetId: budgetToExclude);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateNoOverlap_TwoExisting_RangeOverlapsSecond_FindsOverlap()
    {
        // Arrange
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());

        var oldBudget = new Budget(
            new BudgetId(Guid.NewGuid()),
            userId,
            categoryId,
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 12, 31),
            BudgetPeriod.Monthly,
            new Money(300m, "EUR"));

        var activeBudget = new Budget(
            new BudgetId(Guid.NewGuid()),
            userId,
            categoryId,
            new DateOnly(2026, 1, 1),
            effectiveUntil: null,
            BudgetPeriod.Monthly,
            new Money(500m, "EUR"));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(new List<Budget> { oldBudget, activeBudget });

        var service = new BudgetService(_budgetRepoMock.Object);

        // Act & Assert — overlaps with activeBudget (permanent from 2026-01-01)
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.ValidateNoOverlap(
                userId, categoryId,
                from: new DateOnly(2026, 6, 1),
                until: new DateOnly(2026, 12, 31)));

        Assert.Contains("overlap", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ─────────────────────────────────────────────
    // GetStatusLevel (preserved from old tests)
    // ─────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Domain")]
    public void GetStatusLevel_Under60Percent_ReturnsGreen()
    {
        var result = BudgetService.GetStatusLevel(0.50m);
        Assert.Equal(BudgetStatusLevel.Green, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void GetStatusLevel_At75Percent_ReturnsYellow()
    {
        var result = BudgetService.GetStatusLevel(0.75m);
        Assert.Equal(BudgetStatusLevel.Yellow, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void GetStatusLevel_At100Percent_ReturnsRed()
    {
        var result = BudgetService.GetStatusLevel(1.00m);
        Assert.Equal(BudgetStatusLevel.Red, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void GetStatusLevel_Over100Percent_ReturnsOverage()
    {
        var result = BudgetService.GetStatusLevel(1.25m);
        Assert.Equal(BudgetStatusLevel.Overage, result);
    }

    [Theory]
    [Trait("Category", "Domain")]
    [InlineData(0.74, BudgetStatusLevel.Green)]
    [InlineData(0.75, BudgetStatusLevel.Yellow)]
    [InlineData(0.99, BudgetStatusLevel.Yellow)]
    [InlineData(1.00, BudgetStatusLevel.Red)]
    [InlineData(1.01, BudgetStatusLevel.Overage)]
    public void GetStatusLevel_SpecBoundaryValues_ReturnsCorrectStatus(
        double percentageUsed, BudgetStatusLevel expected)
    {
        BudgetStatusLevel result = BudgetService.GetStatusLevel((decimal)percentageUsed);
        Assert.Equal(expected, result);
    }
}
