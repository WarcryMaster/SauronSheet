using Moq;
using Xunit;

using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Commands;

public class DeactivateBudgetCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private DeactivateBudgetCommandHandler CreateHandler()
    {
        return new DeactivateBudgetCommandHandler(
            _budgetRepoMock.Object,
            _userContextMock.Object);
    }

    private void SetupUser(string userId = "user-1")
    {
        _userContextMock.Setup(u => u.UserId).Returns(userId);
    }

    private static Budget CreatePermanentBudget(string userId = "user-1", decimal limit = 500m)
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId(userId),
            new CategoryId(Guid.NewGuid()),
            new DateOnly(2026, 1, 1),
            null, // permanent
            BudgetPeriod.Monthly,
            new Money(limit, "EUR"));
    }

    private static Budget CreateBudgetWithEnd(
        string userId = "user-1", DateOnly? effectiveUntil = null, decimal limit = 500m)
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId(userId),
            new CategoryId(Guid.NewGuid()),
            new DateOnly(2026, 1, 1),
            effectiveUntil,
            BudgetPeriod.Monthly,
            new Money(limit, "EUR"));
    }

    // ── Happy path ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeactivatePermanentBudget_SetsEffectiveUntil()
    {
        // Arrange
        SetupUser("user-1");
        var budget = CreatePermanentBudget("user-1", 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var asOf = new DateOnly(2026, 5, 30);
        var command = new DeactivateBudgetCommand(budget.Id.Value, asOf);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(asOf, budget.EffectiveUntil);
        _budgetRepoMock.Verify(r => r.UpdateAsync(budget), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeactivateActiveBudget_OverwritesEffectiveUntil()
    {
        // Arrange
        SetupUser("user-1");
        var budget = CreateBudgetWithEnd("user-1", new DateOnly(2026, 12, 31), 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var asOf = new DateOnly(2026, 6, 15);
        var command = new DeactivateBudgetCommand(budget.Id.Value, asOf);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — EffectiveUntil should be overwritten to the deactivation date
        Assert.Equal(asOf, budget.EffectiveUntil);
        _budgetRepoMock.Verify(r => r.UpdateAsync(budget), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeactivatePermanentBudget_BudgetNoLongerPermanent()
    {
        // Arrange
        SetupUser("user-1");
        var budget = CreatePermanentBudget("user-1", 500m);
        Assert.Null(budget.EffectiveUntil); // precondition

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new DeactivateBudgetCommand(budget.Id.Value, new DateOnly(2026, 7, 1));
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(budget.EffectiveUntil);
        Assert.Equal(new DateOnly(2026, 7, 1), budget.EffectiveUntil!.Value);
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

        var command = new DeactivateBudgetCommand(budgetId, new DateOnly(2026, 5, 30));
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
        var budget = CreatePermanentBudget("user-B", 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        var command = new DeactivateBudgetCommand(
            budget.Id.Value, new DateOnly(2026, 5, 30));
        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeactivateBeforeEffectiveFrom_ThrowsDomainException()
    {
        // Arrange
        SetupUser("user-1");
        var budget = CreateBudgetWithEnd("user-1", null, 500m);
        // budget.EffectiveFrom = 2026-01-01

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        // Deactivate with a date BEFORE EffectiveFrom — should be rejected by domain
        var command = new DeactivateBudgetCommand(
            budget.Id.Value, new DateOnly(2025, 12, 31));
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
        var budget = CreatePermanentBudget("user-A", 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new DeactivateBudgetCommand(
            budget.Id.Value, new DateOnly(2026, 6, 30));
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _budgetRepoMock.Verify(r => r.UpdateAsync(It.Is<Budget>(b =>
            b.UserId.Value == "user-A")), Times.Once);
    }
}
