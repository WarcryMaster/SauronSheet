using SauronSheet.Domain.Common;
using Moq;
using Xunit;

using SauronSheet.Application.Features.Transactions.DTOs;
using SauronSheet.Application.Features.Transactions.Queries;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Transactions.Queries;

/// <summary>
/// Tests for SearchTransactionsQueryHandler.
/// Phase 4 (US5): Multi-filter transaction search with pagination.
/// </summary>
public class SearchTransactionsQueryTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly SearchTransactionsQueryHandler _handler;

    public SearchTransactionsQueryTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns("user-1");
        _handler = new SearchTransactionsQueryHandler(
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _userContextMock.Object);
    }

    private static Transaction CreateTransaction(
        string description, decimal amount, DateTime date,
        CategoryId? categoryId = null)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(amount, "EUR"),
            date,
            description,
            categoryId);
    }

    private void SetupCategories(params Category[] categories)
    {
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(categories.ToList());
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_ByKeyword_FiltersCorrectly()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CreateTransaction("Morning Coffee", -5m, new DateTime(2026, 1, 5)),
            CreateTransaction("Grocery shopping", -50m, new DateTime(2026, 1, 10)),
            CreateTransaction("COFFEE BEANS", -15m, new DateTime(2026, 1, 15)),
            CreateTransaction("Gas station", -40m, new DateTime(2026, 1, 20))
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync((ISpecification<Transaction> spec) =>
            {
                var compiled = spec.Criteria.Compile();
                return transactions.Where(compiled).ToList();
            });
        SetupCategories();

        // Act
        var result = await _handler.Handle(
            new SearchTransactionsQuery(Keyword: "coffee"),
            CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_ByDateRange_FiltersCorrectly()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CreateTransaction("Jan expense", -100m, new DateTime(2026, 1, 15)),
            CreateTransaction("Feb expense", -200m, new DateTime(2026, 2, 15)),
            CreateTransaction("Mar expense", -300m, new DateTime(2026, 3, 15))
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync((ISpecification<Transaction> spec) =>
            {
                var compiled = spec.Criteria.Compile();
                return transactions.Where(compiled).ToList();
            });
        SetupCategories();

        // Act
        var result = await _handler.Handle(
            new SearchTransactionsQuery(FromDate: new DateTime(2026, 1, 1), ToDate: new DateTime(2026, 1, 31)),
            CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_ByCategory_FiltersCorrectly()
    {
        // Arrange
        var catId = new CategoryId(Guid.NewGuid());
        var otherCatId = new CategoryId(Guid.NewGuid());
        var transactions = new List<Transaction>
        {
            CreateTransaction("Food expense", -50m, new DateTime(2026, 1, 5), catId),
            CreateTransaction("Transport", -30m, new DateTime(2026, 1, 10), otherCatId),
            CreateTransaction("More food", -25m, new DateTime(2026, 1, 15), catId)
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync((ISpecification<Transaction> spec) =>
            {
                var compiled = spec.Criteria.Compile();
                return transactions.Where(compiled).ToList();
            });
        SetupCategories();

        // Act
        var result = await _handler.Handle(
            new SearchTransactionsQuery(CategoryId: catId.Value),
            CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_ByAmountRange_FiltersCorrectly()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CreateTransaction("Small expense", -10m, new DateTime(2026, 1, 5)),
            CreateTransaction("Medium expense", -50m, new DateTime(2026, 1, 10)),
            CreateTransaction("Large expense", -200m, new DateTime(2026, 1, 15))
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync((ISpecification<Transaction> spec) =>
            {
                var compiled = spec.Criteria.Compile();
                return transactions.Where(compiled).ToList();
            });
        SetupCategories();

        // Act
        var result = await _handler.Handle(
            new SearchTransactionsQuery(MinAmount: -100m, MaxAmount: -5m),
            CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount); // -10 and -50 are in range [-100, -5]
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_CombinedFilters_AppliesAll()
    {
        // Arrange
        var catId = new CategoryId(Guid.NewGuid());
        var transactions = new List<Transaction>
        {
            CreateTransaction("Coffee morning", -5m, new DateTime(2026, 1, 5), catId),
            CreateTransaction("Coffee evening", -5m, new DateTime(2026, 2, 5), catId),
            CreateTransaction("Tea morning", -3m, new DateTime(2026, 1, 10), catId),
            CreateTransaction("Coffee work", -5m, new DateTime(2026, 1, 15))  // no category
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync((ISpecification<Transaction> spec) =>
            {
                var compiled = spec.Criteria.Compile();
                return transactions.Where(compiled).ToList();
            });
        SetupCategories();

        // Act — keyword=coffee + category + Jan date range
        var result = await _handler.Handle(
            new SearchTransactionsQuery(
                Keyword: "coffee",
                FromDate: new DateTime(2026, 1, 1),
                ToDate: new DateTime(2026, 1, 31),
                CategoryId: catId.Value),
            CancellationToken.None);

        // Assert — only "Coffee morning" matches all filters
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_NoFilters_ReturnsAllUserTransactions()
    {
        // Arrange
        var transactions = Enumerable.Range(0, 5)
            .Select(i => CreateTransaction($"Transaction {i}", -10m * (i + 1),
                DateTime.UtcNow.AddDays(-i)))
            .ToList();

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync((ISpecification<Transaction> spec) =>
            {
                var compiled = spec.Criteria.Compile();
                return transactions.Where(compiled).ToList();
            });
        SetupCategories();

        // Act
        var result = await _handler.Handle(new SearchTransactionsQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_NoResults_ReturnsEmptyPage()
    {
        // Arrange
        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync((ISpecification<Transaction> spec) =>
            {
                return new List<Transaction>();
            });
        SetupCategories();

        // Act
        var result = await _handler.Handle(
            new SearchTransactionsQuery(Keyword: "nonexistent"),
            CancellationToken.None);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SearchTransactions_Paginated_RespectsPageSize()
    {
        // Arrange — 100 transactions
        var transactions = Enumerable.Range(0, 100)
            .Select(i => CreateTransaction($"Transaction {i}", -10m,
                DateTime.UtcNow.AddDays(-i)))
            .ToList();

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync((ISpecification<Transaction> spec) =>
            {
                var compiled = spec.Criteria.Compile();
                return transactions.Where(compiled).ToList();
            });
        SetupCategories();

        // Act
        var result = await _handler.Handle(
            new SearchTransactionsQuery(Page: 2, PageSize: 25),
            CancellationToken.None);

        // Assert
        Assert.Equal(25, result.Items.Count);
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(4, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
    }
}
