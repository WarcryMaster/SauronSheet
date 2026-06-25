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
/// Tests for GetRecentTransactionsQueryHandler.
/// Phase 4 (US5): Recent transactions list on dashboard.
/// DT-1b/DT-1c: SubcategoryName population via batched subcategory lookup.
/// </summary>
public class GetRecentTransactionsQueryTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<ISubcategoryRepository> _subcategoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly GetRecentTransactionsQueryHandler _handler;

    public GetRecentTransactionsQueryTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns("user-1");
        // Default: no subcategories unless overridden by individual tests (DT-1b/DT-1c)
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Subcategory>());
        _handler = new GetRecentTransactionsQueryHandler(
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _subcategoryRepoMock.Object,
            _userContextMock.Object);
    }

    private static Transaction CreateTransaction(DateTime date, int index = 0, SubcategoryId? subcategoryId = null)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(-50m - index, "EUR"),
            date,
            $"Transaction {index}",
            subcategoryId: subcategoryId);
    }

    // -------------------------------------------------------------------------
    // DT-1b: SubcategoryName populated when SubcategoryId != null (regression)
    // -------------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetRecentTransactions_TransactionWithSubcategoryId_PopulatesSubcategoryName()
    {
        // DT-1b: handler MUST populate SubcategoryName from batched subcategory lookup
        // Arrange
        var subcategoryId = new SubcategoryId(Guid.NewGuid());
        var categoryId = new CategoryId(Guid.NewGuid());

        var subcategory = new Subcategory(
            subcategoryId,
            new UserId("user-1"),
            categoryId,
            SubcategoryName.Create("Ropa"),
            isAutoCreated: false);

        var transaction = CreateTransaction(DateTime.UtcNow, subcategoryId: subcategoryId);

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { transaction });
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Category>());
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Subcategory> { subcategory });

        // Act
        var result = await _handler.Handle(new GetRecentTransactionsQuery(10), CancellationToken.None);

        // Assert — DT-1b: SubcategoryName MUST be "Ropa", NOT null
        Assert.Single(result);
        Assert.Equal("Ropa", result[0].SubcategoryName);
    }

    // -------------------------------------------------------------------------
    // DT-1c: SubcategoryName null when SubcategoryId == null (triangulation)
    // -------------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetRecentTransactions_TransactionWithNullSubcategoryId_SubcategoryNameIsNull()
    {
        // DT-1c: handler MUST return null SubcategoryName when SubcategoryId is null
        // Arrange — transaction with no subcategory
        var transaction = CreateTransaction(DateTime.UtcNow); // subcategoryId defaults to null

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { transaction });
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Category>());
        // _subcategoryRepoMock default (empty list) is inherited from constructor

        // Act
        var result = await _handler.Handle(new GetRecentTransactionsQuery(10), CancellationToken.None);

        // Assert — DT-1c: SubcategoryName MUST be null when no subcategory assigned
        Assert.Single(result);
        Assert.Null(result[0].SubcategoryName);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetRecentTransactions_ReturnsLastN()
    {
        // Arrange — 20 transactions
        var transactions = Enumerable.Range(0, 20)
            .Select(i => CreateTransaction(DateTime.UtcNow.AddDays(-i), i))
            .ToList();

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _handler.Handle(new GetRecentTransactionsQuery(10), CancellationToken.None);

        // Assert
        Assert.Equal(10, result.Count);
        // Should be ordered by date descending (most recent first)
        Assert.True(result[0].Date >= result[1].Date);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetRecentTransactions_FewerThanN_ReturnsAll()
    {
        // Arrange — only 3 transactions
        var transactions = Enumerable.Range(0, 3)
            .Select(i => CreateTransaction(DateTime.UtcNow.AddDays(-i), i))
            .ToList();

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _handler.Handle(new GetRecentTransactionsQuery(10), CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetRecentTransactions_NoTransactions_ReturnsEmptyList()
    {
        // Arrange
        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _handler.Handle(new GetRecentTransactionsQuery(10), CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    // -------------------------------------------------------------------------
    // TZ-7: DTO Date must be converted to Spain local time
    // -------------------------------------------------------------------------
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetRecentTransactions_DtoDate_ConvertsToSpainLocal()
    {
        // Arrange — create a transaction with a known UTC date in winter (CET = UTC+1)
        var utcDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        // In CET (January), 00:00 UTC → 01:00 Spain
        var expectedSpainHour = 1;

        var transaction = CreateTransaction(utcDate);

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction> { transaction });

        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _handler.Handle(new GetRecentTransactionsQuery(10), CancellationToken.None);

        // Assert — Date must be converted to Spain local
        Assert.Single(result);
        var dto = result[0];
        Assert.Equal(expectedSpainHour, dto.Date.Hour);
        Assert.Equal(15, dto.Date.Day);
        Assert.Equal(1, dto.Date.Month);
        Assert.Equal(2024, dto.Date.Year);
    }
}
