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

public class GetBudgetMetricsQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly BudgetCalculationService _calcService = new();

    private GetBudgetMetricsQueryHandler CreateHandler()
    {
        return new GetBudgetMetricsQueryHandler(
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

    private static Category CreateCategory(Guid id, string name, string userId = "user-1")
    {
        return new Category(
            new CategoryId(id),
            new UserId(userId),
            new CategoryName(name),
            CategoryType.Expense,
            new ColorHex("#000000"),
            "default");
    }

    private static Transaction CreateTransaction(
        Guid categoryId,
        decimal amount,
        DateTime date,
        string userId = "user-1")
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId(userId),
            new Money(amount, "EUR"),
            date,
            "Test transaction",
            new CategoryId(categoryId));
    }

    // ── RED: Returns metrics for budgets with transactions ─────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_WithBudgetsAndSpending_ReturnsMetrics()
    {
        // Arrange
        SetupUser("user-1");
        var catId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var budget = CreateBudget(
            budgetId,
            categoryId: catId,
            limit: 500m,
            period: BudgetPeriod.Annual,
            effectiveFrom: new DateOnly(2026, 1, 1));

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget> { budget });

        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Category>
            {
                CreateCategory(catId, "Groceries")
            });

        var txn = CreateTransaction(catId, 300m, new DateTime(2026, 3, 15));
        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { txn });

        var query = new GetBudgetMetricsQuery(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31));
        var handler = CreateHandler();

        // Act
        List<BudgetMetricsDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert — Annual budget: 1 period × 500 = 500 AccumulatedLimit
        Assert.Single(result);
        BudgetMetricsDto metric = result[0];
        Assert.Equal(budgetId, metric.BudgetId);
        Assert.Equal(catId, metric.CategoryId);
        Assert.Equal("Groceries", metric.CategoryName);
        Assert.Equal(500m, metric.AccumulatedLimit);
        Assert.Equal(300m, metric.Spent);
        Assert.Equal(200m, metric.Remaining);
        Assert.Equal(60m, metric.PercentageUsed);
        Assert.Equal("Green", metric.StatusLevel);
    }

    // ── RED: Includes categories without budgets ───────────────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_CategoriesWithoutBudget_ShowsSinPresupuesto()
    {
        // Arrange
        SetupUser("user-1");
        var uncategorizedId = Guid.NewGuid();

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget>());

        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Category>
            {
                CreateCategory(uncategorizedId, "Entertainment")
            });

        var txn = CreateTransaction(uncategorizedId, 100m, new DateTime(2026, 3, 1));
        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { txn });

        var query = new GetBudgetMetricsQuery(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31));
        var handler = CreateHandler();

        // Act
        List<BudgetMetricsDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert — category without budget appears with "Sin presupuesto"
        Assert.NotEmpty(result);
        BudgetMetricsDto noBudgetMetric = result[0];
        Assert.Equal(Guid.Empty, noBudgetMetric.BudgetId);
        Assert.Equal("Entertainment", noBudgetMetric.CategoryName);
        Assert.Equal(0m, noBudgetMetric.AccumulatedLimit);
        Assert.Equal(100m, noBudgetMetric.Spent);
    }

    // ── RED: Returns empty when no budgets and no transactions ──

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_NoBudgetsNoTransactions_ReturnsEmpty()
    {
        // Arrange
        SetupUser("user-1");

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget>());

        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Category>());

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());

        var query = new GetBudgetMetricsQuery(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31));
        var handler = CreateHandler();

        // Act
        List<BudgetMetricsDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    // ── RED: Multiple budgets with different status levels ──────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_MultipleBudgets_CalculatesEachCorrectly()
    {
        // Arrange
        SetupUser("user-1");
        var catId1 = Guid.NewGuid();
        var catId2 = Guid.NewGuid();
        var budgetId1 = Guid.NewGuid();
        var budgetId2 = Guid.NewGuid();

        // Annual budgets so AccumulatedLimit = Limit for the full year range
        var budget1 = CreateBudget(budgetId1,
            categoryId: catId1, limit: 500m, period: BudgetPeriod.Annual);
        var budget2 = CreateBudget(budgetId2,
            categoryId: catId2, limit: 200m, period: BudgetPeriod.Annual);

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget> { budget1, budget2 });

        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Category>
            {
                CreateCategory(catId1, "Groceries"),
                CreateCategory(catId2, "Transport")
            });

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>
            {
                CreateTransaction(catId1, 200m, new DateTime(2026, 5, 1)),
                CreateTransaction(catId2, 250m, new DateTime(2026, 5, 1))
            });

        var query = new GetBudgetMetricsQuery(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31));
        var handler = CreateHandler();

        // Act
        List<BudgetMetricsDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        // Budget 1: Annual limit 500, spent 200 → 40%, Green
        BudgetMetricsDto metric1 = result.First(m => m.BudgetId == budgetId1);
        Assert.Equal(40m, metric1.PercentageUsed);
        Assert.Equal("Green", metric1.StatusLevel);
        Assert.Equal(500m, metric1.AccumulatedLimit);
        // Budget 2: Annual limit 200, spent 250 → 125%, Overage
        BudgetMetricsDto metric2 = result.First(m => m.BudgetId == budgetId2);
        Assert.Equal(125m, metric2.PercentageUsed);
        Assert.Equal("Overage", metric2.StatusLevel);
        Assert.Equal(200m, metric2.AccumulatedLimit);
    }
}
