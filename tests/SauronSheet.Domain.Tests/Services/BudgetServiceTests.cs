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

    // === ValidateUniqueBudget Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateUniqueBudget_NoDuplicate_Succeeds()
    {
        // Arrange
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var period = new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(userId, categoryId, period))
            .ReturnsAsync((Budget?)null);

        var service = new BudgetService(_budgetRepoMock.Object);

        // Act & Assert — no exception thrown
        await service.ValidateUniqueBudget(userId, categoryId, period);

        _budgetRepoMock.Verify(
            r => r.GetByUserAndCategoryAndMonthAsync(userId, categoryId, period),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateUniqueBudget_DuplicateExists_ThrowsDomainException()
    {
        // Arrange
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var period = new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        var existingBudget = new Budget(
            new BudgetId(Guid.NewGuid()),
            userId,
            categoryId,
            period,
            new Money(500, "EUR"));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(userId, categoryId, period))
            .ReturnsAsync(existingBudget);

        var service = new BudgetService(_budgetRepoMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.ValidateUniqueBudget(userId, categoryId, period));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateUniqueBudget_SameUserDifferentCategory_Succeeds()
    {
        // Arrange
        var userId = new UserId("user-1");
        var categoryA = new CategoryId(Guid.NewGuid());
        var categoryB = new CategoryId(Guid.NewGuid());
        var period = new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(userId, categoryB, period))
            .ReturnsAsync((Budget?)null);

        var service = new BudgetService(_budgetRepoMock.Object);

        // Act & Assert — no exception
        await service.ValidateUniqueBudget(userId, categoryB, period);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public async Task ValidateUniqueBudget_SameUserSameCategoryDifferentMonth_Succeeds()
    {
        // Arrange
        var userId = new UserId("user-1");
        var categoryId = new CategoryId(Guid.NewGuid());
        var marchPeriod = new DateRange(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(userId, categoryId, marchPeriod))
            .ReturnsAsync((Budget?)null);

        var service = new BudgetService(_budgetRepoMock.Object);

        // Act & Assert — no exception
        await service.ValidateUniqueBudget(userId, categoryId, marchPeriod);
    }

    // === GetStatusLevel Tests ===

    [Fact]
    [Trait("Category", "Domain")]
    public void GetStatusLevel_Under60Percent_ReturnsGreen()
    {
        // Arrange & Act
        var result = BudgetService.GetStatusLevel(0.50m);

        // Assert
        Assert.Equal(BudgetStatusLevel.Green, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void GetStatusLevel_At60Percent_ReturnsGreen()
    {
        // 0.60 is NOT > 0.6, so it stays Green
        var result = BudgetService.GetStatusLevel(0.60m);
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
    public void GetStatusLevel_At80Percent_ReturnsYellow()
    {
        // 0.80 is NOT > 0.8, so it stays Yellow
        var result = BudgetService.GetStatusLevel(0.80m);
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

    // === GetStatusLevel Spec Boundary Tests ===
    // Spec thresholds: Green < 75%, Yellow 75%–<100%, Red = 100% exactly, Overage > 100%.
    // Boundary values: 0.74 (just below Green→Yellow), 0.75 (Green→Yellow threshold),
    //                  0.99 (just below Yellow→Red), 1.00 (Yellow→Red threshold),
    //                  1.01 (just above Red→Overage).

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
        // Act
        BudgetStatusLevel result = BudgetService.GetStatusLevel((decimal)percentageUsed);

        // Assert
        Assert.Equal(expected, result);
    }
}
