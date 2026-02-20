using Xunit;
using Moq;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Commands;

public class CreateBudgetCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private CreateBudgetCommandHandler CreateHandler()
    {
        var budgetService = new BudgetService(_budgetRepoMock.Object);
        return new CreateBudgetCommandHandler(
            _budgetRepoMock.Object,
            _categoryRepoMock.Object,
            budgetService,
            _userContextMock.Object);
    }

    private void SetupUser(string userId = "user-1")
    {
        _userContextMock.Setup(u => u.UserId).Returns(userId);
    }

    private Category CreateCategory(CategoryId categoryId, UserId userId)
    {
        return new Category(
            categoryId,
            userId,
            "Groceries",
            null,
            null);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ValidBudget_CreatesBudgetAndReturnsId()
    {
        // Arrange
        SetupUser();
        var categoryId = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var category = CreateCategory(categoryId, userId);

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(
                It.IsAny<UserId>(), It.IsAny<CategoryId>(), It.IsAny<DateRange>()))
            .ReturnsAsync((Budget?)null);

        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        _budgetRepoMock.Verify(r => r.AddAsync(It.Is<Budget>(b =>
            b.Limit.Amount == 500m &&
            b.CategoryId == categoryId &&
            b.UserId.Value == "user-1")), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DuplicateBudget_ThrowsDomainException()
    {
        // Arrange
        SetupUser();
        var categoryId = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var category = CreateCategory(categoryId, userId);
        var period = new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        var existingBudget = new Budget(
            new BudgetId(Guid.NewGuid()), userId, categoryId, period, new Money(500));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(userId, categoryId, period))
            .ReturnsAsync(existingBudget);

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_CategoryNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        SetupUser();
        var categoryId = new CategoryId(Guid.NewGuid());

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ZeroLimit_ThrowsDomainException()
    {
        // Arrange
        SetupUser();
        var categoryId = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var category = CreateCategory(categoryId, userId);

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(
                It.IsAny<UserId>(), It.IsAny<CategoryId>(), It.IsAny<DateRange>()))
            .ReturnsAsync((Budget?)null);

        var command = new CreateBudgetCommand(
            categoryId.Value, 0m,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_TenantScoped_UsesCurrentUserContext()
    {
        // Arrange
        SetupUser("user-A");
        var categoryId = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-A");
        var category = CreateCategory(categoryId, userId);

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(
                It.IsAny<UserId>(), It.IsAny<CategoryId>(), It.IsAny<DateRange>()))
            .ReturnsAsync((Budget?)null);

        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _budgetRepoMock.Verify(r => r.AddAsync(It.Is<Budget>(b =>
            b.UserId.Value == "user-A")), Times.Once);
    }
}
