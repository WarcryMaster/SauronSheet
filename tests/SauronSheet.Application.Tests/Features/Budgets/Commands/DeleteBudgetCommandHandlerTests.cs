using Xunit;
using Moq;
using MediatR;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Commands;

public class DeleteBudgetCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private DeleteBudgetCommandHandler CreateHandler()
    {
        return new DeleteBudgetCommandHandler(
            _budgetRepoMock.Object,
            _userContextMock.Object);
    }

    private Budget CreateBudget(string userId = "user-1")
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId(userId),
            new CategoryId(Guid.NewGuid()),
            new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)),
            new Money(500, "EUR"));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ValidDelete_RemovesBudget()
    {
        // Arrange
        _userContextMock.Setup(u => u.UserId).Returns("user-1");
        var budget = CreateBudget("user-1");

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.DeleteAsync(budget.Id))
            .Returns(Task.CompletedTask);

        var command = new DeleteBudgetCommand(budget.Id.Value);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        _budgetRepoMock.Verify(r => r.DeleteAsync(budget.Id), Times.Once);
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

        var command = new DeleteBudgetCommand(budgetId);
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
        var budget = CreateBudget("user-B");

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        var command = new DeleteBudgetCommand(budget.Id.Value);
        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeletedBudget_DoesNotAffectTransactions()
    {
        // Arrange
        _userContextMock.Setup(u => u.UserId).Returns("user-1");
        var budget = CreateBudget("user-1");

        _budgetRepoMock
            .Setup(r => r.GetByIdAsync(budget.Id))
            .ReturnsAsync(budget);

        _budgetRepoMock
            .Setup(r => r.DeleteAsync(budget.Id))
            .Returns(Task.CompletedTask);

        var command = new DeleteBudgetCommand(budget.Id.Value);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — only budget repo called for delete, no transaction repo interaction
        _budgetRepoMock.Verify(r => r.GetByIdAsync(budget.Id), Times.Once);
        _budgetRepoMock.Verify(r => r.DeleteAsync(budget.Id), Times.Once);
        _budgetRepoMock.VerifyNoOtherCalls();
    }
}
