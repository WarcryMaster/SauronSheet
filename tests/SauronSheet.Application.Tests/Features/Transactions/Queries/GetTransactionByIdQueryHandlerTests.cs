using Moq;
using Xunit;
using SauronSheet.Domain.Common;
using SauronSheet.Application.Features.Transactions.Queries;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Application.Tests.Common;

namespace SauronSheet.Application.Tests.Features.Transactions.Queries;

[Trait("Category", "Application")]
public class GetTransactionByIdQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<ISubcategoryRepository> _mockSubcategoryRepo;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly GetTransactionByIdQueryHandler _handler;
    private readonly UserId _userId = new("user-123");
    private readonly UserId _otherUserId = new("user-456");

    public GetTransactionByIdQueryHandlerTests()
    {
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockSubcategoryRepo = new Mock<ISubcategoryRepository>();
        _mockUserContext = new Mock<IUserContext>();
        _mockUserContext.Setup(x => x.UserId).Returns(_userId.Value);

        _handler = new GetTransactionByIdQueryHandler(
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
            _mockSubcategoryRepo.Object,
            _mockUserContext.Object);
    }

    private static Transaction CreateTransaction(
        UserId userId,
        CategoryId? categoryId = null,
        SubcategoryId? subcategoryId = null,
        CategorySource categorySource = CategorySource.Legacy)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            userId,
            new Money(100.50m, "EUR"),
            new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            "Test transaction",
            categoryId: categoryId,
            subcategoryId: subcategoryId,
            categorySource: categorySource);
    }

    [Fact]
    public async Task Handle_TransactionExists_ReturnsDtoWithCategoryNames()
    {
        // Arrange
        Guid categoryIdGuid = Guid.NewGuid();
        Guid subcategoryIdGuid = Guid.NewGuid();
        CategoryId categoryId = new(categoryIdGuid);
        SubcategoryId subcategoryId = new(subcategoryIdGuid);

        Transaction transaction = CreateTransaction(_userId, categoryId, subcategoryId, CategorySource.UserOverride);

        Category category = TestCategoryFactory.CreateUserCategory(
            categoryId: categoryId,
            userId: _userId,
            name: "Food");

        Subcategory subcategory = TestSubcategoryFactory.CreateUserSubcategory(
            subcategoryId: subcategoryId,
            userId: _userId,
            categoryId: categoryId,
            name: "Restaurants");

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(transaction.Id))
            .ReturnsAsync(transaction);

        _mockCategoryRepo
            .Setup(x => x.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _mockSubcategoryRepo
            .Setup(x => x.GetByIdAsync(subcategoryId))
            .ReturnsAsync(subcategory);

        GetTransactionByIdQuery query = new(transaction.Id.Value);

        // Act
        Application.Features.Transactions.DTOs.TransactionDto result =
            await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(transaction.Id.Value, result.Id);
        Assert.Equal(100.50m, result.Amount);
        Assert.Equal("EUR", result.Currency);
        Assert.Equal("Test transaction", result.Description);
        Assert.Equal(categoryIdGuid, result.CategoryId);
        Assert.Equal("Food", result.CategoryName);
        Assert.Equal(subcategoryIdGuid.ToString(), result.SubcategoryId);
        Assert.Equal("Restaurants", result.SubcategoryName);
        Assert.Equal("UserOverride", result.CategorySource);
    }

    [Fact]
    public async Task Handle_TransactionNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        Guid transactionIdGuid = Guid.NewGuid();

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(new TransactionId(transactionIdGuid)))
            .ReturnsAsync((Transaction?)null);

        GetTransactionByIdQuery query = new(transactionIdGuid);

        // Act & Assert
        EntityNotFoundException exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));

        Assert.Equal("Transaction", exception.EntityName);
        Assert.Equal(transactionIdGuid, exception.EntityId);
    }

    [Fact]
    public async Task Handle_TransactionBelongsToAnotherUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        Transaction transaction = CreateTransaction(_otherUserId);

        _mockTransactionRepo
            .Setup(x => x.GetByIdAsync(transaction.Id))
            .ReturnsAsync(transaction);

        GetTransactionByIdQuery query = new(transaction.Id.Value);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
    }
}
