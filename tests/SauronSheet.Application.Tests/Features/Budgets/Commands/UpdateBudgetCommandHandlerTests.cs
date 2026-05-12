using SauronSheet.Domain.Common;
using Xunit;
using Moq;
using MediatR;

using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Commands;

public class UpdateBudgetCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private UpdateBudgetCommandHandler CreateHandler()
    {
        return new UpdateBudgetCommandHandler(
            _budgetRepoMock.Object,
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
    public async Task Handle_ValidUpdate_UpdatesLimitAndPersists()
    {
        // Arrange
        _userContextMock.Setup(u => u.UserId).Returns("user-1");
        var budget = CreateBudget("user-1", 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateBudgetCommand(budget.Id.Value, 600m);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.Equal(600m, budget.Limit.Amount);
        _budgetRepoMock.Verify(r => r.UpdateAsync(budget), Times.Once);
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

        var command = new UpdateBudgetCommand(budgetId, 600m);
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
        _userContextMock.Setup(u => u.UserId).Returns("user-A");
        var budget = CreateBudget("user-B", 500m);

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        var command = new UpdateBudgetCommand(budget.Id.Value, 600m);
        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }
}
