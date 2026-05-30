using Moq;
using Xunit;

using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Commands;

public class UpdateBudgetEffectiveDatesCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private UpdateBudgetEffectiveDatesCommandHandler CreateHandler()
    {
        var budgetService = new BudgetService(_budgetRepoMock.Object);
        return new UpdateBudgetEffectiveDatesCommandHandler(
            _budgetRepoMock.Object,
            budgetService,
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

    // ── Happy path ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ValidDateUpdate_PersistsNewDates()
    {
        // Arrange
        SetupUser("user-1");
        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var budget = CreateBudget(
            budgetId,
            categoryId: categoryId,
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 6, 30));

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync(budget);

        // No existing budgets for overlap check
        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(
                new UserId("user-1"), new CategoryId(categoryId)))
            .ReturnsAsync(Array.Empty<Budget>());

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(budget))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetEffectiveDatesCommand(
            budgetId,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 12, 31));

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(new DateOnly(2026, 7, 1), budget.EffectiveFrom);
        Assert.Equal(new DateOnly(2026, 12, 31), budget.EffectiveUntil);
        _budgetRepoMock.Verify(r => r.UpdateAsync(budget), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ChangeToPermanent_EffectiveUntilNull()
    {
        // Arrange
        SetupUser("user-1");
        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var budget = CreateBudget(
            budgetId,
            categoryId: categoryId,
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 6, 30));

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(
                new UserId("user-1"), new CategoryId(categoryId)))
            .ReturnsAsync(Array.Empty<Budget>());

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(budget))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetEffectiveDatesCommand(
            budgetId,
            new DateOnly(2026, 7, 1),
            null); // make it permanent

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Null(budget.EffectiveUntil);
        _budgetRepoMock.Verify(r => r.UpdateAsync(budget), Times.Once);
    }

    // ── Ownership validation ────────────────────────────────────

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

        var command = new UpdateBudgetEffectiveDatesCommand(
            budgetId,
            new DateOnly(2026, 1, 1),
            null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_BudgetOwnedByOtherUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        SetupUser("user-A");
        var budgetId = Guid.NewGuid();
        var budget = CreateBudget(
            budgetId,
            userId: "user-B");

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync(budget);

        var command = new UpdateBudgetEffectiveDatesCommand(
            budgetId,
            new DateOnly(2026, 1, 1),
            null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    // ── Overlap validation ──────────────────────────────────────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_OverlappingDateRange_ThrowsDomainException()
    {
        // Arrange
        SetupUser("user-1");
        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var budget = CreateBudget(
            budgetId,
            categoryId: categoryId,
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 3, 31));

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync(budget);

        // Existing budget that overlaps with the new proposed range
        var existingBudget = CreateBudget(
            Guid.NewGuid(),
            categoryId: categoryId,
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: null); // permanent — overlaps with anything

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(
                new UserId("user-1"), new CategoryId(categoryId)))
            .ReturnsAsync(new[] { existingBudget });

        var command = new UpdateBudgetEffectiveDatesCommand(
            budgetId,
            new DateOnly(2026, 6, 1),
            null); // would overlap the permanent existing budget

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_AdjacentDateRange_NoOverlap_Succeeds()
    {
        // Arrange
        SetupUser("user-1");
        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var budget = CreateBudget(
            budgetId,
            categoryId: categoryId,
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 3, 31));

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync(budget);

        // Existing budget ending right before the new range starts
        var existingBudget = CreateBudget(
            Guid.NewGuid(),
            categoryId: categoryId,
            effectiveFrom: new DateOnly(2025, 7, 1),
            effectiveUntil: new DateOnly(2025, 12, 31));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(
                new UserId("user-1"), new CategoryId(categoryId)))
            .ReturnsAsync(new[] { existingBudget });

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(budget))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetEffectiveDatesCommand(
            budgetId,
            new DateOnly(2026, 4, 1),  // starts right after existing ends
            new DateOnly(2026, 12, 31));

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _budgetRepoMock.Verify(r => r.UpdateAsync(budget), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SameBudgetIsExcluded_NoSelfOverlap()
    {
        // Arrange
        SetupUser("user-1");
        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var budget = CreateBudget(
            budgetId,
            categoryId: categoryId,
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: null);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync(budget);

        // Only this budget exists (same ID), should be excluded
        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(
                new UserId("user-1"), new CategoryId(categoryId)))
            .ReturnsAsync(new[] { budget });

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(budget))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetEffectiveDatesCommand(
            budgetId,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 12, 31));

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — no overlap exception because the same budget is excluded
        _budgetRepoMock.Verify(r => r.UpdateAsync(budget), Times.Once);
    }

    // ── Domain validation (delegated to Budget entity) ──────────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_EffectiveUntilBeforeEffectiveFrom_ThrowsDomainException()
    {
        // Arrange
        SetupUser("user-1");
        var budgetId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var budget = CreateBudget(
            budgetId,
            categoryId: categoryId,
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 6, 30));

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAsync(
                new UserId("user-1"), new CategoryId(categoryId)))
            .ReturnsAsync(Array.Empty<Budget>());

        var command = new UpdateBudgetEffectiveDatesCommand(
            budgetId,
            new DateOnly(2026, 12, 1),
            new DateOnly(2026, 6, 1)); // until before from

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
    }
}
