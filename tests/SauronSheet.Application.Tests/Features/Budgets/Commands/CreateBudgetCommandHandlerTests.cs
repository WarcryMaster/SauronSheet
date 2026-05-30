using Moq;
using Xunit;

using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Application.Tests.Common;
using SauronSheet.Domain.Common;
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

    private static Category CreateCategory(CategoryId categoryId, UserId userId, string name = "Groceries")
    {
        return TestCategoryFactory.CreateUserCategory(categoryId: categoryId, userId: userId, name: name);
    }

    // ── Happy path ──────────────────────────────────────────────

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
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(Array.Empty<Budget>());

        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new CreateBudgetCommand(
            categoryId.Value,
            500m,
            new DateOnly(2026, 1, 1),
            null,
            BudgetPeriod.Monthly);

        var handler = CreateHandler();

        // Act
        Guid result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        _budgetRepoMock.Verify(r => r.AddAsync(It.Is<Budget>(b =>
            b.Limit.Amount == 500m &&
            b.CategoryId == categoryId &&
            b.UserId.Value == "user-1" &&
            b.PeriodGranularity == BudgetPeriod.Monthly)), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_PermanentBudget_EffectiveUntilIsNull()
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
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(Array.Empty<Budget>());

        Budget? captured = null;
        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>()))
            .Callback<Budget>(b => captured = b)
            .Returns(Task.CompletedTask);

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateOnly(2026, 1, 1), null, BudgetPeriod.Monthly);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Null(captured!.EffectiveUntil);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_AnnualBudget_CreatesWithCorrectGranularity()
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
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(Array.Empty<Budget>());

        Budget? captured = null;
        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>()))
            .Callback<Budget>(b => captured = b)
            .Returns(Task.CompletedTask);

        var command = new CreateBudgetCommand(
            categoryId.Value, 6000m,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            BudgetPeriod.Annual);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(BudgetPeriod.Annual, captured!.PeriodGranularity);
        Assert.Equal(6000m, captured.Limit.Amount);
        Assert.Equal(new DateOnly(2026, 12, 31), captured.EffectiveUntil);
    }

    // ── Category validation ────────────────────────────────────

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
            new DateOnly(2026, 1, 1), null, BudgetPeriod.Monthly);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_CategoryNotOwnedByUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        SetupUser("user-A");
        var categoryId = new CategoryId(Guid.NewGuid());
        var otherUserId = new UserId("user-B");
        var category = CreateCategory(categoryId, otherUserId);

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateOnly(2026, 1, 1), null, BudgetPeriod.Monthly);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    // ── Overlap validation ─────────────────────────────────────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_OverlappingBudget_ThrowsDomainException()
    {
        // Arrange
        SetupUser();
        var categoryId = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var category = CreateCategory(categoryId, userId);

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        var existingBudget = new Budget(
            new BudgetId(Guid.NewGuid()),
            userId,
            categoryId,
            new DateOnly(2026, 1, 1),
            null, // permanent
            BudgetPeriod.Monthly,
            new Money(300m));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(new[] { existingBudget });

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateOnly(2026, 6, 1), null, BudgetPeriod.Monthly);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_AdjacentBudget_NoOverlap_Allowed()
    {
        // Arrange
        SetupUser();
        var categoryId = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var category = CreateCategory(categoryId, userId);

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        var existingBudget = new Budget(
            new BudgetId(Guid.NewGuid()),
            userId,
            categoryId,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 6, 30),
            BudgetPeriod.Monthly,
            new Money(300m));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(new[] { existingBudget });

        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateOnly(2026, 7, 1), null,
            BudgetPeriod.Monthly);

        var handler = CreateHandler();

        // Act
        Guid result = await handler.Handle(command, CancellationToken.None);

        // Assert — should not throw
        Assert.NotEqual(Guid.Empty, result);
        _budgetRepoMock.Verify(r => r.AddAsync(It.IsAny<Budget>()), Times.Once);
    }

    // ── Limit validation (delegated to Budget entity) ──────────

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
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(Array.Empty<Budget>());

        var command = new CreateBudgetCommand(
            categoryId.Value, 0m,
            new DateOnly(2026, 1, 1), null, BudgetPeriod.Monthly);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_NegativeLimit_ThrowsDomainException()
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
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(Array.Empty<Budget>());

        var command = new CreateBudgetCommand(
            categoryId.Value, -100m,
            new DateOnly(2026, 1, 1), null, BudgetPeriod.Monthly);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    // ── Tenant scoping ─────────────────────────────────────────

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
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(Array.Empty<Budget>());

        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateOnly(2026, 1, 1), null, BudgetPeriod.Monthly);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _budgetRepoMock.Verify(r => r.AddAsync(It.Is<Budget>(b =>
            b.UserId.Value == "user-A")), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_LargeLimit_Succeeds()
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
            .Setup(r => r.GetByUserAndCategoryAsync(userId, categoryId))
            .ReturnsAsync(Array.Empty<Budget>());

        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new CreateBudgetCommand(
            categoryId.Value, 999999999.99m,
            new DateOnly(2026, 1, 1), null, BudgetPeriod.Annual);

        var handler = CreateHandler();

        // Act
        Guid result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        _budgetRepoMock.Verify(r => r.AddAsync(It.Is<Budget>(b =>
            b.Limit.Amount == 999999999.99m)), Times.Once);
    }
}
