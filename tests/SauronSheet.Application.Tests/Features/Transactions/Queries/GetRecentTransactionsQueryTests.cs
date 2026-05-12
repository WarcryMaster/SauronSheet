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
/// </summary>
public class GetRecentTransactionsQueryTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly GetRecentTransactionsQueryHandler _handler;

    public GetRecentTransactionsQueryTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns("user-1");
        _handler = new GetRecentTransactionsQueryHandler(
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _userContextMock.Object);
    }

    private static Transaction CreateTransaction(DateTime date, int index = 0)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(-50m - index, "EUR"),
            date,
            $"Transaction {index}");
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
}
