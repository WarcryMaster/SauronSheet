using Moq;
using Xunit;

using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Queries;

public class GetBudgetHistoryQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly BudgetCalculationService _calcService = new();

    private GetBudgetHistoryQueryHandler CreateHandler()
    {
        return new GetBudgetHistoryQueryHandler(
            _budgetRepoMock.Object,
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _calcService,
            _userContextMock.Object);
    }

    private void SetupUser(string userId = "user-1")
    {
        _userContextMock.Setup(u => u.UserId).Returns(userId);
    }

    private static Budget CreateBudget(
        Guid id,
        string userId = "user-1",
        Guid? categoryId = null,
        DateOnly? effectiveFrom = null,
        DateOnly? effectiveUntil = null,
        BudgetPeriod period = BudgetPeriod.Monthly,
        decimal limit = 500m)
    {
        return new Budget(
            new BudgetId(id),
            new UserId(userId),
            new CategoryId(categoryId ?? Guid.NewGuid()),
            effectiveFrom ?? new DateOnly(2026, 1, 1),
            effectiveUntil,
            period,
            new Money(limit, "EUR"));
    }

    private static Transaction CreateTransaction(
        Guid categoryId, decimal amount, DateTime date,
        string userId = "user-1")
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId(userId),
            new Money(amount, "EUR"),
            date, "Test transaction",
            new CategoryId(categoryId));
    }

    // ── RED: Returns history for budgets active in the year ────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_YearWithActiveBudgets_ReturnsSummaries()
    {
        // Arrange
        SetupUser("user-1");
        var catId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var budget = CreateBudget(budgetId,
            categoryId: catId,
            limit: 500m,
            period: BudgetPeriod.Monthly,
            effectiveFrom: new DateOnly(2026, 1, 1));

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget> { budget });

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>
            {
                CreateTransaction(catId, 200m, new DateTime(2026, 3, 15))
            });

        var query = new GetBudgetHistoryQuery(2026);
        var handler = CreateHandler();

        // Act
        List<BudgetPeriodSummaryDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert — 12 monthly periods for the year
        Assert.NotEmpty(result);
    }

    // ── RED: Returns empty when no budgets active in year ──────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_NoBudgetsInYear_ReturnsEmpty()
    {
        // Arrange
        SetupUser("user-1");

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget>());

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());

        var query = new GetBudgetHistoryQuery(2026);
        var handler = CreateHandler();

        // Act
        List<BudgetPeriodSummaryDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    // ── RED: Monthly budget shows 12 period summaries for a full year

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_MonthlyBudget_Returns12PeriodSummaries()
    {
        // Arrange
        SetupUser("user-1");
        var catId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var budget = CreateBudget(budgetId,
            categoryId: catId,
            limit: 500m,
            period: BudgetPeriod.Monthly,
            effectiveFrom: new DateOnly(2026, 1, 1));

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget> { budget });

        // Transactions spread across the year
        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>
            {
                CreateTransaction(catId, 100m, new DateTime(2026, 1, 15)),
                CreateTransaction(catId, 200m, new DateTime(2026, 6, 15)),
            });

        var query = new GetBudgetHistoryQuery(2026);
        var handler = CreateHandler();

        // Act
        List<BudgetPeriodSummaryDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert — 12 monthly entries for the year
        Assert.Equal(12, result.Count);
        // First period: January
        Assert.Contains("Enero", result[0].Period);
    }
}
