using Xunit;
using Moq;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Application.Tests.Common;

namespace SauronSheet.Application.Tests.Features.Budgets.Queries;

public class GetBudgetByIdQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private GetBudgetByIdQueryHandler CreateHandler()
    {
        return new GetBudgetByIdQueryHandler(
            _budgetRepoMock.Object,
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _userContextMock.Object);
    }

    private Budget CreateBudget(string userId = "user-1", decimal limit = 500m)
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId(userId),
            new CategoryId(Guid.NewGuid()),
            new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)),
            new Money(limit, "EUR"));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetExists_ReturnsBudgetStatusDto()
    {
        // Arrange
        _userContextMock.Setup(u => u.UserId).Returns("user-1");
        var userId = new UserId("user-1");
        var catId = new CategoryId(Guid.NewGuid());
        var budget = new Budget(
            new BudgetId(Guid.NewGuid()), userId, catId,
            new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)),
            new Money(500, "EUR"));

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        var category = TestCategoryFactory.CreateUserCategory(categoryId: catId, userId: userId, name: "Groceries", color: "#00FF00");
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(catId))
            .ReturnsAsync(category);

        // Transaction: -200 EUR spend
        var expense = new Transaction(
            new TransactionId(Guid.NewGuid()), userId,
            new Money(-200, "EUR"), new DateTime(2026, 2, 10), "Shopping", catId);

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { expense });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new GetBudgetByIdQuery(budget.Id.Value), CancellationToken.None);

        // Assert
        Assert.Equal(budget.Id.Value, result.Id);
        Assert.Equal("Groceries", result.CategoryName);
        Assert.Equal(500m, result.LimitAmount);
        Assert.Equal(200m, result.CurrentSpend);
        Assert.Equal(300m, result.RemainingAmount);
        Assert.Equal(0.40m, result.PercentageUsed);
        Assert.Equal("Green", result.StatusLevel);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        _userContextMock.Setup(u => u.UserId).Returns("user-1");
        var budgetId = Guid.NewGuid();

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync((Budget?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(new GetBudgetByIdQuery(budgetId), CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ZeroTransactions_ReturnsZeroSpend()
    {
        // Arrange
        _userContextMock.Setup(u => u.UserId).Returns("user-1");
        var userId = new UserId("user-1");
        var catId = new CategoryId(Guid.NewGuid());
        var budget = new Budget(
            new BudgetId(Guid.NewGuid()), userId, catId,
            new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)),
            new Money(500, "EUR"));

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        var category = TestCategoryFactory.CreateUserCategory(categoryId: catId, userId: userId, name: "Groceries", color: "#00FF00");
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(catId))
            .ReturnsAsync(category);

        _transactionRepoMock
            .Setup(r => r.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new GetBudgetByIdQuery(budget.Id.Value), CancellationToken.None);

        // Assert
        Assert.Equal(0m, result.CurrentSpend);
        Assert.Equal(0m, result.PercentageUsed);
        Assert.Equal(500m, result.RemainingAmount);
        Assert.Equal("Green", result.StatusLevel);
    }
}
