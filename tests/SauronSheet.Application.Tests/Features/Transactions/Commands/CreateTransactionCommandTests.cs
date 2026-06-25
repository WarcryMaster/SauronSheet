using SauronSheet.Domain.Common;
using Xunit;
using Moq;
using SauronSheet.Application.Features.Transactions.Commands;

using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Application.Tests.Common;

namespace SauronSheet.Application.Tests.Features.Transactions.Commands;

public class CreateTransactionCommandTests
{
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<ICategoryRepository> _mockCategoryRepo;
    private readonly Mock<IUserContext> _mockUserContext;

    public CreateTransactionCommandTests()
    {
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockCategoryRepo = new Mock<ICategoryRepository>();
        _mockUserContext = new Mock<IUserContext>();

        _mockUserContext.Setup(x => x.UserId).Returns("test-user-id");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task CreateTransaction_ValidInput_ReturnsTransactionId()
    {
        // Arrange
        var handler = new CreateTransactionCommandHandler(
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var command = new CreateTransactionCommand(
            Amount: -50.00m,
            Currency: "EUR",
            Date: DateTime.UtcNow.AddDays(-1),
            Description: "Groceries",
            CategoryId: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        _mockTransactionRepo.Verify(x => x.AddAsync(It.IsAny<Transaction>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task CreateTransaction_WithCategory_ValidatesCategory()
    {
        // Arrange
        var handler = new CreateTransactionCommandHandler(
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var categoryId = Guid.NewGuid();
        var userId = new UserId("test-user-id");
        var category = TestCategoryFactory.CreateUserCategory(categoryId: new CategoryId(categoryId), userId: userId, name: "Food");

        _mockCategoryRepo.Setup(x => x.GetByIdAsync(new CategoryId(categoryId)))
            .ReturnsAsync(category);

        var command = new CreateTransactionCommand(
            Amount: -50.00m,
            Currency: "EUR",
            Date: DateTime.UtcNow.AddDays(-1),
            Description: "Groceries",
            CategoryId: categoryId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        _mockCategoryRepo.Verify(x => x.GetByIdAsync(new CategoryId(categoryId)), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task CreateTransaction_InvalidCategory_ThrowsException()
    {
        // Arrange
        var handler = new CreateTransactionCommandHandler(
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        var categoryId = Guid.NewGuid();

        _mockCategoryRepo.Setup(x => x.GetByIdAsync(new CategoryId(categoryId)))
            .ReturnsAsync((Category?)null);

        var command = new CreateTransactionCommand(
            Amount: -50.00m,
            Currency: "EUR",
            Date: DateTime.UtcNow.AddDays(-1),
            Description: "Groceries",
            CategoryId: categoryId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            handler.Handle(command, CancellationToken.None));
    }

    // ── Timezone fix: UTC normalization ────────────────────────────────────────

    /// <summary>
    /// TZ-2: When the command has an Unspecified DateTime, the handler must normalize
    /// it to DateTimeKind.Utc before creating the Transaction entity.
    /// Prevents the timezone bug when the user submits a date from a form.
    /// </summary>
    [Fact]
    [Trait("Category", "Application")]
    public async Task CreateTransaction_NormalizesDateToUtc()
    {
        // Arrange
        var handler = new CreateTransactionCommandHandler(
            _mockTransactionRepo.Object,
            _mockCategoryRepo.Object,
            _mockUserContext.Object);

        // Unspecified kind — like a date from a form input
        var unspecifiedDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Unspecified);
        var command = new CreateTransactionCommand(
            Amount: -50.00m,
            Currency: "EUR",
            Date: unspecifiedDate,
            Description: "Test UTC normalization",
            CategoryId: null);

        Transaction? captured = null;
        _mockTransactionRepo
            .Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => captured = t)
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — date must be UTC Kind
        Assert.NotNull(captured);
        Assert.Equal(DateTimeKind.Utc, captured!.Date.Kind);
        Assert.Equal(15, captured.Date.Day);
        Assert.Equal(6, captured.Date.Month);
        Assert.Equal(2024, captured.Date.Year);
    }
}
