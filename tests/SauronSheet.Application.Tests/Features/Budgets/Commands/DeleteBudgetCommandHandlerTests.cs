using System.Net.Http;
using Moq;
using Xunit;
using SauronSheet.Domain.Common;
using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Application.Tests.Features.Budgets.Commands;

[Trait("Category", "Application")]
public class DeleteBudgetCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _mockBudgetRepo;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly DeleteBudgetCommandHandler _handler;
    private readonly UserId _userId = new("user-123");
    private readonly UserId _otherUserId = new("user-456");

    public DeleteBudgetCommandHandlerTests()
    {
        _mockBudgetRepo = new Mock<IBudgetRepository>();
        _mockUserContext = new Mock<IUserContext>();
        _mockUserContext.Setup(x => x.UserId).Returns(_userId.Value);

        _handler = new DeleteBudgetCommandHandler(
            _mockBudgetRepo.Object,
            _mockUserContext.Object);
    }

    private static Budget CreateBudget(UserId userId)
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()),
            userId,
            new CategoryId(Guid.NewGuid()),
            new DateOnly(2026, 1, 1),
            null,
            BudgetPeriod.Monthly,
            new Money(500m, "EUR"));
    }

    [Fact]
    public async Task Handle_BudgetExistsAndOwnedByUser_DeletesBudget()
    {
        // Arrange
        var budgetId = Guid.NewGuid();
        var budget = CreateBudget(_userId);

        _mockBudgetRepo
            .Setup(x => x.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync(budget);

        // Act
        await _handler.Handle(new DeleteBudgetCommand(budgetId), CancellationToken.None);

        // Assert
        _mockBudgetRepo.Verify(x => x.GetByIdAsync(new BudgetId(budgetId)), Times.Once);
        _mockBudgetRepo.Verify(x => x.DeleteAsync(new BudgetId(budgetId)), Times.Once);
    }

    [Fact]
    public async Task Handle_BudgetNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var budgetId = Guid.NewGuid();

        _mockBudgetRepo
            .Setup(x => x.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync((Budget?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _handler.Handle(new DeleteBudgetCommand(budgetId), CancellationToken.None));

        Assert.Contains("Budget", ex.Message);
        _mockBudgetRepo.Verify(x => x.DeleteAsync(It.IsAny<BudgetId>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BudgetBelongsToAnotherUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        var budgetId = Guid.NewGuid();
        var budget = CreateBudget(_otherUserId);

        _mockBudgetRepo
            .Setup(x => x.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync(budget);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _handler.Handle(new DeleteBudgetCommand(budgetId), CancellationToken.None));

        _mockBudgetRepo.Verify(x => x.DeleteAsync(It.IsAny<BudgetId>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HttpRequestException_ThrowsDomainExceptionWithNetworkMessage()
    {
        // Arrange
        var budgetId = Guid.NewGuid();
        var budget = CreateBudget(_userId);

        _mockBudgetRepo
            .Setup(x => x.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync(budget);

        _mockBudgetRepo
            .Setup(x => x.DeleteAsync(new BudgetId(budgetId)))
            .ThrowsAsync(new HttpRequestException("Network failure"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _handler.Handle(new DeleteBudgetCommand(budgetId), CancellationToken.None));

        Assert.Contains("network error", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_GenericException_ThrowsDomainExceptionWithUnexpectedMessage()
    {
        // Arrange
        var budgetId = Guid.NewGuid();
        var budget = CreateBudget(_userId);

        _mockBudgetRepo
            .Setup(x => x.GetByIdAsync(new BudgetId(budgetId)))
            .ReturnsAsync(budget);

        _mockBudgetRepo
            .Setup(x => x.DeleteAsync(new BudgetId(budgetId)))
            .ThrowsAsync(new InvalidOperationException("Something broke"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _handler.Handle(new DeleteBudgetCommand(budgetId), CancellationToken.None));

        Assert.Contains("unexpected error", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
