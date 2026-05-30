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

public class GetBudgetVsActualQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly BudgetCalculationService _calcService = new();

    private GetBudgetVsActualQueryHandler CreateHandler()
    {
        return new GetBudgetVsActualQueryHandler(
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

    // ── RED: Returns comparison for category with budget ───────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_WithBudget_UsesCalculationService()
    {
        // Arrange
        SetupUser("user-1");
        var catId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var budget = CreateBudget(budgetId,
            categoryId: catId,
            limit: 300m,
            period: BudgetPeriod.Monthly,
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

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>
            {
                CreateTransaction(catId, 150m, new DateTime(2026, 1, 15))
            });

        var query = new GetBudgetVsActualQuery(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));
        var handler = CreateHandler();

        // Act
        List<BudgetVsActualDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert — Monthly: 1 period × 300 = 300 budget limit, spent 150
        Assert.Single(result);
        Assert.Equal("Groceries", result[0].CategoryName);
        Assert.Equal(300m, result[0].BudgetLimit);
        Assert.Equal(150m, result[0].ActualSpend);
        Assert.Equal(150m, result[0].Difference);
        Assert.Equal(50m, result[0].PercentageUsed);
    }

    // ── RED: Category without budget shows "Sin presupuesto" ────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_NoBudget_ShowsSinPresupuesto()
    {
        // Arrange
        SetupUser("user-1");
        var catId = Guid.NewGuid();

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget>());

        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Category>
            {
                CreateCategory(catId, "Entertainment")
            });

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>
            {
                CreateTransaction(catId, 50m, new DateTime(2026, 1, 15))
            });

        var query = new GetBudgetVsActualQuery(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));
        var handler = CreateHandler();

        // Act
        List<BudgetVsActualDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Entertainment", result[0].CategoryName);
        Assert.Null(result[0].BudgetLimit);
        Assert.Equal(50m, result[0].ActualSpend);
        Assert.Equal("Sin presupuesto", result[0].StatusLevel);
    }

    // ── RED: Multiple categories, mixed with and without budget ──

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_Mixed_ReturnsAllCategories()
    {
        // Arrange
        SetupUser("user-1");
        var catId1 = Guid.NewGuid();
        var catId2 = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var budget = CreateBudget(budgetId,
            categoryId: catId1, limit: 200m,
            period: BudgetPeriod.Monthly);

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget> { budget });

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
                CreateTransaction(catId1, 100m, new DateTime(2026, 1, 15)),
                CreateTransaction(catId2, 80m, new DateTime(2026, 1, 20))
            });

        var query = new GetBudgetVsActualQuery(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));
        var handler = CreateHandler();

        // Act
        List<BudgetVsActualDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert — 2 categories, one with budget, one without
        Assert.Equal(2, result.Count);
        BudgetVsActualDto withBudget = result.First(r => r.BudgetLimit != null);
        Assert.NotNull(withBudget.BudgetLimit);
        BudgetVsActualDto withoutBudget = result.First(r => r.BudgetLimit == null);
        Assert.Null(withoutBudget.BudgetLimit);
        Assert.Equal(80m, withoutBudget.ActualSpend);
        Assert.Equal("Sin presupuesto", withoutBudget.StatusLevel);
    }
}
