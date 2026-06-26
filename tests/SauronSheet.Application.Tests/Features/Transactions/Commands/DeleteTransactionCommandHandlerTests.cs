using Moq;
using Xunit;
using SauronSheet.Domain.Common;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Application.Tests.Features.Transactions.Commands;

[Trait("Category", "Application")]
public class DeleteTransactionCommandHandlerTests
{
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly DeleteTransactionCommandHandler _handler;
    private readonly UserId _userId = new("user-123");
    private readonly UserId _otherUserId = new("user-456");

    public DeleteTransactionCommandHandlerTests()
    {
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockUserContext = new Mock<IUserContext>();
        _mockUserContext.Setup(x => x.UserId).Returns(_userId.Value);

        _handler = new DeleteTransactionCommandHandler(
            _mockTransactionRepo.Object,
            _mockUserContext.Object);
    }

    private static Transaction CreateTransaction(UserId userId)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            userId,
            new Money(100m, "EUR"),
            DateTime.UtcNow,
            "Test transaction");
    }

    [Fact]
    public async Task Handle_TransactionExistsAndOwnedByUser_DeletesTransaction()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = CreateTransaction(_userId);

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(new TransactionId(transactionId)))
            .ReturnsAsync(transaction);

        // Act
        await _handler.Handle(new DeleteTransactionCommand(transactionId), CancellationToken.None);

        // Assert
        _mockTransactionRepo.Verify(x => x.GetByIdAsync(new TransactionId(transactionId)), Times.Once);
        _mockTransactionRepo.Verify(x => x.DeleteAsync(new TransactionId(transactionId)), Times.Once);
    }

    [Fact]
    public async Task Handle_TransactionNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(new TransactionId(transactionId)))
            .ReturnsAsync((Transaction?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _handler.Handle(new DeleteTransactionCommand(transactionId), CancellationToken.None));

        Assert.Contains("Transaction", ex.Message);
        Assert.Contains(transactionId.ToString(), ex.Message);
        _mockTransactionRepo.Verify(x => x.DeleteAsync(It.IsAny<TransactionId>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TransactionBelongsToAnotherUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = CreateTransaction(_otherUserId);

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(new TransactionId(transactionId)))
            .ReturnsAsync(transaction);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _handler.Handle(new DeleteTransactionCommand(transactionId), CancellationToken.None));

        _mockTransactionRepo.Verify(x => x.DeleteAsync(It.IsAny<TransactionId>()), Times.Never);
    }
}
