using Moq;
using Xunit;
using SauronSheet.Domain.Common;
using SauronSheet.Application.Features.Transactions.Commands;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Application.Tests.Common;

namespace SauronSheet.Application.Tests.Features.Transactions.Commands;

[Trait("Category", "Application")]
public class UpdateTransactionCategoryCommandHandlerTests
{
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly UpdateTransactionCategoryCommandHandler _handler;
    private readonly UserId _userId = new("user-123");
    private readonly UserId _otherUserId = new("user-456");

    public UpdateTransactionCategoryCommandHandlerTests()
    {
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockUserContext = new Mock<IUserContext>();
        _mockUserContext.Setup(x => x.UserId).Returns(_userId.Value);

        _handler = new UpdateTransactionCategoryCommandHandler(
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
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
    public async Task Handle_AssignCategory_UpdatesTransactionWithCategory()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transaction = CreateTransaction(_userId);
        var category = TestCategoryFactory.CreateUserCategory(
            categoryId: new CategoryId(categoryId),
            userId: _userId,
            name: "Food");

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(new TransactionId(transactionId)))
            .ReturnsAsync(transaction);

        _mockCategoryRepo
            .Setup(x => x.GetByIdAsync(new CategoryId(categoryId)))
            .ReturnsAsync(category);

        // Act
        await _handler.Handle(
            new UpdateTransactionCategoryCommand(transactionId, categoryId),
            CancellationToken.None);

        // Assert
        Assert.Equal(new CategoryId(categoryId), transaction.CategoryId);
        Assert.Equal(CategorySource.UserOverride, transaction.CategorySource);
        _mockTransactionRepo.Verify(x => x.UpdateAsync(transaction), Times.Once);
    }

    [Fact]
    public async Task Handle_ClearCategory_SetsCategoryToNull()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = CreateTransaction(_userId);

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(new TransactionId(transactionId)))
            .ReturnsAsync(transaction);

        // Act
        await _handler.Handle(
            new UpdateTransactionCategoryCommand(transactionId, null),
            CancellationToken.None);

        // Assert
        Assert.Null(transaction.CategoryId);
        _mockTransactionRepo.Verify(x => x.UpdateAsync(transaction), Times.Once);
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
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _handler.Handle(
                new UpdateTransactionCategoryCommand(transactionId, Guid.NewGuid()),
                CancellationToken.None));

        _mockTransactionRepo.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
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
            _handler.Handle(
                new UpdateTransactionCategoryCommand(transactionId, Guid.NewGuid()),
                CancellationToken.None));

        _mockTransactionRepo.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transaction = CreateTransaction(_userId);

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(new TransactionId(transactionId)))
            .ReturnsAsync(transaction);

        _mockCategoryRepo
            .Setup(x => x.GetByIdAsync(new CategoryId(categoryId)))
            .ReturnsAsync((Category?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _handler.Handle(
                new UpdateTransactionCategoryCommand(transactionId, categoryId),
                CancellationToken.None));

        _mockTransactionRepo.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CategoryBelongsToAnotherUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transaction = CreateTransaction(_userId);
        var category = TestCategoryFactory.CreateUserCategory(
            categoryId: new CategoryId(categoryId),
            userId: _otherUserId,
            name: "Other's Category");

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(new TransactionId(transactionId)))
            .ReturnsAsync(transaction);

        _mockCategoryRepo
            .Setup(x => x.GetByIdAsync(new CategoryId(categoryId)))
            .ReturnsAsync(category);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _handler.Handle(
                new UpdateTransactionCategoryCommand(transactionId, categoryId),
                CancellationToken.None));

        _mockTransactionRepo.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
    }
}
