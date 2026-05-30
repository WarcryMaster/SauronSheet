using Moq;
using Xunit;

using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Commands;

public class UpdateBudgetLimitCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private UpdateBudgetLimitCommandHandler CreateHandler()
    {
        return new UpdateBudgetLimitCommandHandler(
            _budgetRepoMock.Object,
            _userContextMock.Object);
    }

    private void SetupUser(string userId = "user-1")
    {
        _userContextMock.Setup(u => u.UserId).Returns(userId);
    }

    private static Budget CreateBudget(string userId = "user-1", decimal limit = 500m)
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId(userId),
            new CategoryId(Guid.NewGuid()),
            new DateOnly(2026, 1, 1),
            null,
            BudgetPeriod.Monthly,
            new Money(limit, "EUR"));
    }

    // ── Happy path ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ValidUpdate_UpdatesLimitAndPersists()
    {
        // Arrange
        SetupUser("user-1");
        var budget = CreateBudget("user-1", 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetLimitCommand(budget.Id.Value, 600m);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(600m, budget.Limit.Amount);
        _budgetRepoMock.Verify(r => r.UpdateAsync(budget), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_IncreaseLimit_Succeeds()
    {
        // Arrange
        SetupUser("user-1");
        var budget = CreateBudget("user-1", 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetLimitCommand(budget.Id.Value, 1000m);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1000m, budget.Limit.Amount);
        _budgetRepoMock.Verify(r => r.UpdateAsync(budget), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DecreaseLimit_StillPositive_Succeeds()
    {
        // Arrange
        SetupUser("user-1");
        var budget = CreateBudget("user-1", 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetLimitCommand(budget.Id.Value, 100m);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(100m, budget.Limit.Amount);
        _budgetRepoMock.Verify(r => r.UpdateAsync(budget), Times.Once);
    }

    // ── Error handling ─────────────────────────────────────────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        SetupUser("user-1");
        var budgetId = Guid.NewGuid();

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync((Budget?)null);

        var command = new UpdateBudgetLimitCommand(budgetId, 600m);
        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DifferentUserBudget_ThrowsEntityNotFoundException()
    {
        // Arrange
        SetupUser("user-A");
        var budget = CreateBudget("user-B", 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        var command = new UpdateBudgetLimitCommand(budget.Id.Value, 600m);
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
        SetupUser("user-1");
        var budget = CreateBudget("user-1", 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        var command = new UpdateBudgetLimitCommand(budget.Id.Value, 0m);
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
        SetupUser("user-1");
        var budget = CreateBudget("user-1", 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        var command = new UpdateBudgetLimitCommand(budget.Id.Value, -1m);
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
        var budget = CreateBudget("user-A", 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetLimitCommand(budget.Id.Value, 700m);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _budgetRepoMock.Verify(r => r.UpdateAsync(It.Is<Budget>(b =>
            b.UserId.Value == "user-A")), Times.Once);
    }
}
