using SauronSheet.Domain.Common;
using Xunit;
using Moq;

using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Application.Tests.Common;

namespace SauronSheet.Application.Tests.Features.Budgets.Queries;

public class GetBudgetVsActualQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private GetBudgetVsActualQueryHandler CreateHandler()
    {
        return new GetBudgetVsActualQueryHandler(
            _budgetRepoMock.Object,
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _userContextMock.Object);
    }

    private void SetupUser(string userId = "user-1")
    {
        _userContextMock.Setup(u => u.UserId).Returns(userId);
    }

    private Budget CreateBudget(UserId userId, CategoryId catId, decimal limit = 500m)
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()), userId, catId,
            new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)),
            new Money(limit, "EUR"));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetsAndTransactions_ReturnsComparison()
    {
        // Arrange
        SetupUser();
        var userId = new UserId("user-1");
        var cat1Id = new CategoryId(Guid.NewGuid());
        var cat2Id = new CategoryId(Guid.NewGuid());

        var budget1 = CreateBudget(userId, cat1Id, 500m);
        var budget2 = CreateBudget(userId, cat2Id, 300m);

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Budget> { budget1, budget2 });

        var cat1 = TestCategoryFactory.CreateUserCategory(categoryId: cat1Id, userId: userId, name: "Groceries");
        var cat2 = TestCategoryFactory.CreateUserCategory(categoryId: cat2Id, userId: userId, name: "Entertainment");
        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Category> { cat1, cat2 });

        var tx1 = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-200, "EUR"), new DateTime(2026, 2, 10), "Shopping", cat1Id);
        var tx2 = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-150, "EUR"), new DateTime(2026, 2, 15), "Cinema", cat2Id);

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { tx1, tx2 });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetBudgetVsActualQuery(2026, 2), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.NotNull(dto.BudgetLimit));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_CategoryWithSpendButNoBudget_ShowsNoBudget()
    {
        // Arrange
        SetupUser();
        var userId = new UserId("user-1");
        var budgetedCatId = new CategoryId(Guid.NewGuid());
        var unbudgetedCatId = new CategoryId(Guid.NewGuid());

        var budget = CreateBudget(userId, budgetedCatId, 500m);

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Budget> { budget });

        var cat1 = TestCategoryFactory.CreateUserCategory(categoryId: budgetedCatId, userId: userId, name: "Groceries");
        var cat2 = TestCategoryFactory.CreateUserCategory(categoryId: unbudgetedCatId, userId: userId, name: "Transport");
        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Category> { cat1, cat2 });

        var tx1 = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-200, "EUR"), new DateTime(2026, 2, 10), "Shopping", budgetedCatId);
        var tx2 = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-50, "EUR"), new DateTime(2026, 2, 12), "Bus", unbudgetedCatId);

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { tx1, tx2 });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetBudgetVsActualQuery(2026, 2), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        var unbudgeted = result.First(r => r.CategoryName == "Transport");
        Assert.Null(unbudgeted.BudgetLimit);
        Assert.Equal(50m, unbudgeted.ActualSpend);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetWithNoSpend_ShowsZeroActual()
    {
        // Arrange
        SetupUser();
        var userId = new UserId("user-1");
        var catId = new CategoryId(Guid.NewGuid());
        var budget = CreateBudget(userId, catId, 500m);

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Budget> { budget });

        var cat = TestCategoryFactory.CreateUserCategory(categoryId: catId, userId: userId, name: "Groceries");
        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Category> { cat });

        // No transactions
        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetBudgetVsActualQuery(2026, 2), CancellationToken.None);

        // Assert
        var dto = Assert.Single(result);
        Assert.Equal(0m, dto.ActualSpend);
        Assert.Equal(500m, dto.BudgetLimit);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SummaryRow_TotalsCorrectly()
    {
        // Arrange
        SetupUser();
        var userId = new UserId("user-1");
        var cat1Id = new CategoryId(Guid.NewGuid());
        var cat2Id = new CategoryId(Guid.NewGuid());

        var budget1 = CreateBudget(userId, cat1Id, 500m);
        var budget2 = CreateBudget(userId, cat2Id, 300m);

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Budget> { budget1, budget2 });

        var cat1 = TestCategoryFactory.CreateUserCategory(categoryId: cat1Id, userId: userId, name: "A");
        var cat2 = TestCategoryFactory.CreateUserCategory(categoryId: cat2Id, userId: userId, name: "B");
        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Category> { cat1, cat2 });

        var tx1 = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-200, "EUR"), new DateTime(2026, 2, 10), "Exp1", cat1Id);
        var tx2 = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-150, "EUR"), new DateTime(2026, 2, 15), "Exp2", cat2Id);

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { tx1, tx2 });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetBudgetVsActualQuery(2026, 2), CancellationToken.None);

        // Assert — verify individual budget items and their sums
        var totalBudgeted = result.Where(r => r.BudgetLimit.HasValue).Sum(r => r.BudgetLimit!.Value);
        var totalActual = result.Sum(r => r.ActualSpend);

        Assert.Equal(800m, totalBudgeted); // 500 + 300
        Assert.Equal(350m, totalActual);   // 200 + 150
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SortOrder_OverBudgetFirst()
    {
        // Arrange
        SetupUser();
        var userId = new UserId("user-1");
        var underCatId = new CategoryId(Guid.NewGuid());
        var overCatId = new CategoryId(Guid.NewGuid());

        var underBudget = CreateBudget(userId, underCatId, 500m); // will be under
        var overBudget = CreateBudget(userId, overCatId, 100m);   // will be over

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Budget> { underBudget, overBudget });

        var cat1 = TestCategoryFactory.CreateUserCategory(categoryId: underCatId, userId: userId, name: "Under");
        var cat2 = TestCategoryFactory.CreateUserCategory(categoryId: overCatId, userId: userId, name: "Over");
        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Category> { cat1, cat2 });

        var txUnder = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-100, "EUR"), new DateTime(2026, 2, 10), "Exp1", underCatId);
        var txOver = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-200, "EUR"), new DateTime(2026, 2, 15), "Exp2", overCatId);

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { txUnder, txOver });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetBudgetVsActualQuery(2026, 2), CancellationToken.None);

        // Assert — over-budget should be first
        Assert.Equal("Over", result[0].CategoryName);
        Assert.True(result[0].PercentageUsed > 1.0m);
    }
}
