using Xunit;
using Moq;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Queries;

public class GetBudgetsQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private GetBudgetsQueryHandler CreateHandler()
    {
        return new GetBudgetsQueryHandler(
            _budgetRepoMock.Object,
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _userContextMock.Object);
    }

    private void SetupUser(string userId = "user-1")
    {
        _userContextMock.Setup(u => u.UserId).Returns(userId);
    }

    private Budget CreateBudget(UserId userId, CategoryId catId, decimal limit = 500m, int month = 2)
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()),
            userId,
            catId,
            new DateRange(new DateTime(2026, month, 1), new DateTime(2026, month, DateTime.DaysInMonth(2026, month))),
            new Money(limit, "EUR"));
    }

    private Category CreateCategory(CategoryId id, UserId userId, string name, string? color = null)
    {
        return new Category(id, userId, name, color, null);
    }

    private Transaction CreateExpense(UserId userId, CategoryId catId, decimal amount, DateTime date)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            userId,
            new Money(-Math.Abs(amount), "EUR"),
            date,
            "Expense",
            catId);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetsExist_ReturnsBudgetStatusDtoList()
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

        var cat1 = CreateCategory(cat1Id, userId, "Groceries", "#00FF00");
        var cat2 = CreateCategory(cat2Id, userId, "Entertainment", "#FF0000");

        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Category> { cat1, cat2 });

        // Transactions: cat1 has 200 spend, cat2 has 100 spend
        var txs1 = new List<Transaction>
        {
            CreateExpense(userId, cat1Id, 200m, new DateTime(2026, 2, 10))
        };
        var txs2 = new List<Transaction>
        {
            CreateExpense(userId, cat2Id, 100m, new DateTime(2026, 2, 15))
        };

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync((ISpecification<Transaction> spec) =>
            {
                // Return different transactions based on the spec — simplified: return all and let handler filter
                return txs1.Concat(txs2).ToList();
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetBudgetsQuery(2026, 2), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        // Sorted alphabetically: Entertainment before Groceries
        Assert.Equal("Entertainment", result[0].CategoryName);
        Assert.Equal("Groceries", result[1].CategoryName);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_NoBudgets_ReturnsEmptyList()
    {
        // Arrange
        SetupUser();
        var userId = new UserId("user-1");

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Budget>());

        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Category>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetBudgetsQuery(2026, 2), CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_WithYearMonthFilter_FiltersCorrectly()
    {
        // Arrange
        SetupUser();
        var userId = new UserId("user-1");
        var catId = new CategoryId(Guid.NewGuid());

        var febBudget = CreateBudget(userId, catId, 500m, 2);
        var marBudget = CreateBudget(userId, catId, 600m, 3);

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Budget> { febBudget, marBudget });

        var cat = CreateCategory(catId, userId, "Groceries");
        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Category> { cat });

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());

        var handler = CreateHandler();

        // Act — filter to Feb only
        var result = await handler.Handle(new GetBudgetsQuery(2026, 2), CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(new DateTime(2026, 2, 1), result[0].PeriodStart);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_TenantScoped_ReturnsOnlyOwnBudgets()
    {
        // Arrange
        SetupUser("user-A");
        var userA = new UserId("user-A");
        var catId = new CategoryId(Guid.NewGuid());

        var budget = CreateBudget(userA, catId, 500m);

        // Repository returns only user-A budgets (tenant scoping via repo)
        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(userA))
            .ReturnsAsync(new List<Budget> { budget });

        var cat = CreateCategory(catId, userA, "Food");
        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(userA))
            .ReturnsAsync(new List<Category> { cat });

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetBudgetsQuery(2026, 2), CancellationToken.None);

        // Assert
        Assert.Single(result);
        _budgetRepoMock.Verify(r => r.GetByUserIdAsync(userA), Times.Once);
    }
}
