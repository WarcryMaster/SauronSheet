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
public class UpdateTransactionCommandHandlerTests
{
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<ISubcategoryRepository> _mockSubcategoryRepo;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly UpdateTransactionCommandHandler _handler;
    private readonly UserId _userId = new("user-123");
    private readonly UserId _otherUserId = new("user-456");

    public UpdateTransactionCommandHandlerTests()
    {
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockSubcategoryRepo = new Mock<ISubcategoryRepository>();
        _mockUserContext = new Mock<IUserContext>();
        _mockUserContext.Setup(x => x.UserId).Returns(_userId.Value);

        _handler = new UpdateTransactionCommandHandler(
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
            _mockSubcategoryRepo.Object,
            _mockUserContext.Object);
    }

    private static Transaction CreateTestTransaction(
        TransactionId? id = null,
        UserId? userId = null,
        CategoryId? categoryId = null,
        decimal? balance = null)
    {
        return new Transaction(
            id ?? new TransactionId(Guid.NewGuid()),
            userId ?? new UserId("user-123"),
            new Money(100m, "EUR"),
            DateTime.UtcNow,
            "Test description",
            categoryId: categoryId,
            balance: balance);
    }

    private static UpdateTransactionCommand CreateCommand(
        TransactionId? transactionId = null,
        decimal amount = 100m,
        CategoryId? categoryId = null,
        SubcategoryId? subcategoryId = null)
    {
        TransactionId resolvedId = transactionId ?? new TransactionId(Guid.NewGuid());
        return new UpdateTransactionCommand(
            resolvedId,
            amount,
            "EUR",
            DateTime.UtcNow,
            "Updated description",
            categoryId,
            subcategoryId);
    }

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesTransactionAndPersists()
    {
        // Arrange
        TransactionId transactionId = new TransactionId(Guid.NewGuid());
        CategoryId categoryId = new CategoryId(Guid.NewGuid());
        Transaction transaction = CreateTestTransaction(id: transactionId, userId: _userId);

        Category category = TestCategoryFactory.CreateUserCategory(
            categoryId: categoryId,
            userId: _userId,
            name: "Food");

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(transactionId))
            .ReturnsAsync(transaction);

        _mockCategoryRepo
            .Setup(x => x.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _mockTransactionRepo
            .Setup(x => x.ExistsDuplicateAsync(
                _userId,
                It.IsAny<DateTime>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<decimal?>()))
            .ReturnsAsync(false);

        UpdateTransactionCommand command = CreateCommand(
            transactionId: transactionId,
            categoryId: categoryId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(categoryId, transaction.CategoryId);
        Assert.Equal(CategorySource.UserOverride, transaction.CategorySource);
        _mockTransactionRepo.Verify(x => x.UpdateAsync(transaction), Times.Once);
    }

    [Fact]
    public async Task Handle_TransactionNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        TransactionId transactionId = new TransactionId(Guid.NewGuid());

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(transactionId))
            .ReturnsAsync((Transaction?)null);

        UpdateTransactionCommand command = CreateCommand(transactionId: transactionId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _mockTransactionRepo.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TransactionBelongsToAnotherUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        TransactionId transactionId = new TransactionId(Guid.NewGuid());
        Transaction transaction = CreateTestTransaction(id: transactionId, userId: _otherUserId);

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(transactionId))
            .ReturnsAsync(transaction);

        UpdateTransactionCommand command = CreateCommand(transactionId: transactionId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _mockTransactionRepo.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateDetected_AllowsUpdate()
    {
        // Arrange
        // Note: Duplicate detection is currently disabled for updates because
        // ExistsDuplicateAsync doesn't support excluding the current transaction.
        // This is a known limitation tracked for future improvement.
        TransactionId transactionId = new TransactionId(Guid.NewGuid());
        Transaction transaction = CreateTestTransaction(id: transactionId, userId: _userId);

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(transactionId))
            .ReturnsAsync(transaction);

        // Duplicate found — but update should still proceed
        _mockTransactionRepo
            .Setup(x => x.ExistsDuplicateAsync(
                _userId,
                It.IsAny<DateTime>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<decimal?>()))
            .ReturnsAsync(true);

        UpdateTransactionCommand command = CreateCommand(transactionId: transactionId);

        // Act & Assert — no exception, update proceeds
        await _handler.Handle(command, CancellationToken.None);

        _mockTransactionRepo.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidSubcategory_ThrowsDomainException()
    {
        // Arrange
        TransactionId transactionId = new TransactionId(Guid.NewGuid());
        CategoryId categoryId = new CategoryId(Guid.NewGuid());
        CategoryId wrongCategoryId = new CategoryId(Guid.NewGuid());
        SubcategoryId subcategoryId = new SubcategoryId(Guid.NewGuid());

        Transaction transaction = CreateTestTransaction(id: transactionId, userId: _userId);

        Category category = TestCategoryFactory.CreateUserCategory(
            categoryId: categoryId,
            userId: _userId,
            name: "Food");

        // Subcategory belongs to a DIFFERENT category
        Subcategory subcategory = TestSubcategoryFactory.CreateUserSubcategory(
            subcategoryId: subcategoryId,
            userId: _userId,
            categoryId: wrongCategoryId,
            name: "Wrong Sub");

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(transactionId))
            .ReturnsAsync(transaction);

        _mockCategoryRepo
            .Setup(x => x.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _mockSubcategoryRepo
            .Setup(x => x.GetByIdAsync(subcategoryId))
            .ReturnsAsync(subcategory);

        UpdateTransactionCommand command = CreateCommand(
            transactionId: transactionId,
            categoryId: categoryId,
            subcategoryId: subcategoryId);

        // Act & Assert
        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            _handler.Handle(command, CancellationToken.None));

        Assert.Contains("does not belong to the selected category", exception.Message);
        _mockTransactionRepo.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
    }
}
