using Moq;
using Xunit;

using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Commands;

public class UpdateBudgetPeriodCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private UpdateBudgetPeriodCommandHandler CreateHandler()
    {
        return new UpdateBudgetPeriodCommandHandler(
            _budgetRepoMock.Object,
            _userContextMock.Object);
    }

    private void SetupUser(string userId = "user-1")
    {
        _userContextMock.Setup(u => u.UserId).Returns(userId);
    }

    private static Budget CreateBudget(
        string userId = "user-1",
        BudgetPeriod granularity = BudgetPeriod.Monthly,
        decimal limit = 500m)
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId(userId),
            new CategoryId(Guid.NewGuid()),
            new DateOnly(2026, 1, 1),
            null,
            granularity,
            new Money(limit, "EUR"));
    }

    // ── Happy path ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ValidUpdate_UpdatesGranularityAndLimit()
    {
        // Arrange
        SetupUser("user-1");
        var budget = CreateBudget("user-1", BudgetPeriod.Monthly, 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetPeriodCommand(
            budget.Id.Value, BudgetPeriod.Annual, 6000m);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(BudgetPeriod.Annual, budget.PeriodGranularity);
        Assert.Equal(6000m, budget.Limit.Amount);
        _budgetRepoMock.Verify(r => r.UpdateAsync(budget), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_MonthlyToQuarterly_UpdatesBoth()
    {
        // Arrange
        SetupUser("user-1");
        var budget = CreateBudget("user-1", BudgetPeriod.Monthly, 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetPeriodCommand(
            budget.Id.Value, BudgetPeriod.Quarterly, 1500m);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(BudgetPeriod.Quarterly, budget.PeriodGranularity);
        Assert.Equal(1500m, budget.Limit.Amount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_AnnualToSemester_UpdatesBoth()
    {
        // Arrange
        SetupUser("user-1");
        var budget = CreateBudget("user-1", BudgetPeriod.Annual, 12000m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetPeriodCommand(
            budget.Id.Value, BudgetPeriod.Semester, 6000m);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(BudgetPeriod.Semester, budget.PeriodGranularity);
        Assert.Equal(6000m, budget.Limit.Amount);
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

        var command = new UpdateBudgetPeriodCommand(
            budgetId, BudgetPeriod.Annual, 6000m);
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
        var budget = CreateBudget("user-B", BudgetPeriod.Monthly, 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        var command = new UpdateBudgetPeriodCommand(
            budget.Id.Value, BudgetPeriod.Annual, 6000m);
        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_InvalidLimit_ThrowsDomainException()
    {
        // Arrange
        SetupUser("user-1");
        var budget = CreateBudget("user-1", BudgetPeriod.Monthly, 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        var command = new UpdateBudgetPeriodCommand(
            budget.Id.Value, BudgetPeriod.Annual, 0m);
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
        var budget = CreateBudget("user-A", BudgetPeriod.Monthly, 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetPeriodCommand(
            budget.Id.Value, BudgetPeriod.Quarterly, 1500m);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _budgetRepoMock.Verify(r => r.UpdateAsync(It.Is<Budget>(b =>
            b.UserId.Value == "user-A")), Times.Once);
    }
}
