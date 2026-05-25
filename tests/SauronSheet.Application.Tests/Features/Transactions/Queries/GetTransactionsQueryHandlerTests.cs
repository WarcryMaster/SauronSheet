using SauronSheet.Domain.Common;
using Moq;
using Xunit;

using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Application.Features.Transactions.Queries;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Transactions.Queries;

/// <summary>
/// Tests for GetTransactionsQueryHandler.
/// DT-1b/DT-1c: SubcategoryName population via batched subcategory lookup (no N+1).
/// DT-1d: Category name resolution via single batch call — no N+1 GetByIdAsync per category.
/// </summary>
public class GetTransactionsQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<ISubcategoryRepository> _subcategoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly GetTransactionsQueryHandler _handler;

    public GetTransactionsQueryHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns("user-1");
        // Default: no categories unless overridden by individual tests (DT-1d)
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Category>());
        // Default: no subcategories unless overridden by individual tests
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Subcategory>());
        _handler = new GetTransactionsQueryHandler(
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _subcategoryRepoMock.Object,
            _userContextMock.Object);
    }

    private static Transaction CreateTransaction(
        string description = "Test transaction",
        SubcategoryId? subcategoryId = null)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(-50m, "EUR"),
            DateTime.UtcNow,
            description,
            subcategoryId: subcategoryId);
    }

    // -------------------------------------------------------------------------
    // DT-1b: SubcategoryName populated when SubcategoryId != null (regression)
    // -------------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactions_TransactionWithSubcategoryId_PopulatesSubcategoryName()
    {
        // DT-1b: handler MUST populate SubcategoryName from batched subcategory lookup
        // Arrange
        var subcategoryId = new SubcategoryId(Guid.NewGuid());
        var categoryId = new CategoryId(Guid.NewGuid());

        var subcategory = new Subcategory(
            subcategoryId,
            new UserId("user-1"),
            categoryId,
            SubcategoryName.Create("Alimentación"),
            isAutoCreated: false);

        var transaction = CreateTransaction(subcategoryId: subcategoryId);

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { transaction });
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Subcategory> { subcategory });

        // Act
        var result = await _handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        // Assert — DT-1b: SubcategoryName MUST be "Alimentación", NOT null
        Assert.Single(result.Items);
        Assert.Equal("Alimentación", result.Items[0].SubcategoryName);
    }

    // -------------------------------------------------------------------------
    // DT-1c: SubcategoryName null when SubcategoryId == null (triangulation)
    // -------------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactions_TransactionWithNullSubcategoryId_SubcategoryNameIsNull()
    {
        // DT-1c: handler MUST return null SubcategoryName when SubcategoryId is null
        // Arrange — transaction without subcategory
        var transaction = CreateTransaction(); // subcategoryId defaults to null

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { transaction });
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Category>());
        // _subcategoryRepoMock default (empty list) inherited from constructor

        // Act
        var result = await _handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        // Assert — DT-1c: SubcategoryName MUST be null
        Assert.Single(result.Items);
        Assert.Null(result.Items[0].SubcategoryName);
    }

    // -------------------------------------------------------------------------
    // DT-1d: Category resolution MUST use single batch call — no N+1
    // -------------------------------------------------------------------------

    [Fact]
    [Trait("Category", "Application")]
    public async Task DT_1d_CategoryResolution_UsesBatchCall()
    {
        // DT-1d: handler MUST call GetByUserIdAsync exactly once and NEVER call GetByIdAsync
        // Arrange — two transactions with different CategoryIds (M=2 > 1)
        var categoryIdA = new CategoryId(Guid.NewGuid());
        var categoryIdB = new CategoryId(Guid.NewGuid());

        var txA = new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(-10m, "EUR"),
            DateTime.UtcNow,
            "Tx A",
            categoryId: categoryIdA);

        var txB = new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(-20m, "EUR"),
            DateTime.UtcNow.AddSeconds(-1),
            "Tx B",
            categoryId: categoryIdB);

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { txA, txB });

        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Category>());

        // Act
        await _handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        // Assert — DT-1d: batch call MUST be used; individual lookup MUST NOT be used
        _categoryRepoMock.Verify(x => x.GetByUserIdAsync(It.IsAny<UserId>()), Times.Once(),
            "GetByUserIdAsync must be called exactly once for batch category loading");
        _categoryRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<CategoryId>()), Times.Never(),
            "GetByIdAsync must never be called — N+1 pattern must be eliminated");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task DT_1d_CategoryResolution_MapsNameCorrectly()
    {
        // DT-1d triangulation: category names MUST be resolved from the batch lookup
        // Arrange — one transaction with a known CategoryId
        var categoryId = new CategoryId(Guid.NewGuid());

        var category = new Category(
            categoryId,
            new UserId("user-1"),
            CategoryName.Create("Alimentación"),
            CategoryType.Expense,
            ColorHex.Create("#FF0000"),
            "food");

        var tx = new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(-50m, "EUR"),
            DateTime.UtcNow,
            "Groceries",
            categoryId: categoryId);

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { tx });

        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Category> { category });

        // Act
        var result = await _handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        // Assert — DT-1d: CategoryName MUST be resolved from batch lookup
        Assert.Single(result.Items);
        Assert.Equal("Alimentación", result.Items[0].CategoryName);
    }
}
