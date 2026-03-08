using Xunit;
using Moq;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Application.Tests.Common;

namespace SauronSheet.Application.Tests.Features.Budgets.Queries;

public class GetBudgetSummaryForDashboardQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private GetBudgetSummaryForDashboardQueryHandler CreateHandler()
    {
        return new GetBudgetSummaryForDashboardQueryHandler(
            _budgetRepoMock.Object,
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _userContextMock.Object);
    }

    private void SetupUser(string userId = "user-1")
    {
        _userContextMock.Setup(u => u.UserId).Returns(userId);
    }

    private Budget CreateBudget(UserId userId, CategoryId catId, decimal limit)
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()), userId, catId,
            new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)),
            new Money(limit, "EUR"));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetsExist_ReturnsAggregatedSummary()
    {
        // Arrange — 3 budgets: 2 on-track, 1 over-budget (Overage)
        SetupUser();
        var userId = new UserId("user-1");
        var cat1Id = new CategoryId(Guid.NewGuid());
        var cat2Id = new CategoryId(Guid.NewGuid());
        var cat3Id = new CategoryId(Guid.NewGuid());

        var budget1 = CreateBudget(userId, cat1Id, 500m); // will have 100 spend → 20% → Green
        var budget2 = CreateBudget(userId, cat2Id, 300m); // will have 150 spend → 50% → Green
        var budget3 = CreateBudget(userId, cat3Id, 200m); // will have 300 spend → 150% → Overage

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Budget> { budget1, budget2, budget3 });

        var cat1 = TestCategoryFactory.CreateUserCategory(categoryId: cat1Id, userId: userId, name: "A");
        var cat2 = TestCategoryFactory.CreateUserCategory(categoryId: cat2Id, userId: userId, name: "B");
        var cat3 = TestCategoryFactory.CreateUserCategory(categoryId: cat3Id, userId: userId, name: "C");
        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Category> { cat1, cat2, cat3 });

        var tx1 = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-100, "EUR"), new DateTime(2026, 2, 5), "Exp1", cat1Id);
        var tx2 = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-150, "EUR"), new DateTime(2026, 2, 10), "Exp2", cat2Id);
        var tx3 = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-300, "EUR"), new DateTime(2026, 2, 15), "Exp3", cat3Id);

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { tx1, tx2, tx3 });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new GetBudgetSummaryForDashboardQuery(2026, 2), CancellationToken.None);

        // Assert
        Assert.Equal(3, result.TotalBudgets);
        Assert.Equal(2, result.OnTrackCount);
        Assert.Equal(1, result.OverBudgetCount);
        Assert.Equal(3, result.Budgets.Count);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_NoBudgets_ReturnsEmptySummary()
    {
        // Arrange
        SetupUser();
        var userId = new UserId("user-1");

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Budget>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new GetBudgetSummaryForDashboardQuery(2026, 2), CancellationToken.None);

        // Assert
        Assert.Equal(0, result.TotalBudgets);
        Assert.Equal(0, result.OnTrackCount);
        Assert.Equal(0, result.OverBudgetCount);
        Assert.Empty(result.Budgets);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_AllOnTrack_NoOverBudget()
    {
        // Arrange — 2 budgets both under 60% → Green
        SetupUser();
        var userId = new UserId("user-1");
        var cat1Id = new CategoryId(Guid.NewGuid());
        var cat2Id = new CategoryId(Guid.NewGuid());

        var budget1 = CreateBudget(userId, cat1Id, 500m);
        var budget2 = CreateBudget(userId, cat2Id, 400m);

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Budget> { budget1, budget2 });

        var cat1 = TestCategoryFactory.CreateUserCategory(categoryId: cat1Id, userId: userId, name: "A");
        var cat2 = TestCategoryFactory.CreateUserCategory(categoryId: cat2Id, userId: userId, name: "B");
        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Category> { cat1, cat2 });

        // Both under 60%
        var tx1 = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-100, "EUR"), new DateTime(2026, 2, 5), "Exp1", cat1Id);
        var tx2 = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-80, "EUR"), new DateTime(2026, 2, 10), "Exp2", cat2Id);

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { tx1, tx2 });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new GetBudgetSummaryForDashboardQuery(2026, 2), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalBudgets);
        Assert.Equal(2, result.OnTrackCount);
        Assert.Equal(0, result.OverBudgetCount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_TenantScoped_OnlyCurrentUserBudgets()
    {
        // Arrange
        SetupUser("user-A");
        var userA = new UserId("user-A");
        var catId = new CategoryId(Guid.NewGuid());
        var budget = CreateBudget(userA, catId, 500m);

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userA))
            .ReturnsAsync(new List<Budget> { budget });

        var cat = TestCategoryFactory.CreateUserCategory(categoryId: catId, userId: userA, name: "Food");
        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(userA))
            .ReturnsAsync(new List<Category> { cat });

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new GetBudgetSummaryForDashboardQuery(2026, 2), CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalBudgets);
        _budgetRepoMock.Verify(r => r.GetByUserIdAsync(userA), Times.Once);
    }
}
