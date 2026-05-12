using SauronSheet.Domain.Common;
using Moq;
using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Analytics.Queries;

/// <summary>
/// Tests for GetTransactionSummaryQueryHandler.
/// Phase 4 (US6): Transaction summary statistics.
/// </summary>
public class GetTransactionSummaryQueryTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly GetTransactionSummaryQueryHandler _handler;

    public GetTransactionSummaryQueryTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns("user-1");
        _handler = new GetTransactionSummaryQueryHandler(
            _transactionRepoMock.Object,
            _userContextMock.Object);
    }

    private static Transaction CreateTransaction(decimal amount, DateTime date)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(amount, "EUR"),
            date,
            "Test transaction");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactionSummary_CalculatesCorrectly()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);
        var transactions = new List<Transaction>
        {
            CreateTransaction(500m, new DateTime(2026, 1, 5)),    // income
            CreateTransaction(200m, new DateTime(2026, 1, 10)),   // income
            CreateTransaction(-300m, new DateTime(2026, 1, 15)),  // expense
            CreateTransaction(-100m, new DateTime(2026, 1, 20)),  // expense
            CreateTransaction(-50m, new DateTime(2026, 1, 25))    // expense
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _handler.Handle(new GetTransactionSummaryQuery(from, to), CancellationToken.None);

        // Assert
        Assert.Equal(700m, result.TotalIncome);
        Assert.Equal(450m, result.TotalExpenses);
        Assert.Equal(250m, result.NetAmount);
        Assert.Equal(5, result.TransactionCount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactionSummary_NoTransactions_ReturnsZeros()
    {
        // Arrange
        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());

        // Act
        var result = await _handler.Handle(
            new GetTransactionSummaryQuery(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow),
            CancellationToken.None);

        // Assert
        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(0m, result.TotalExpenses);
        Assert.Equal(0m, result.NetAmount);
        Assert.Equal(0, result.TransactionCount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactionSummary_OnlyExpenses_NetIsNegative()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CreateTransaction(-300m, new DateTime(2026, 1, 5)),
            CreateTransaction(-200m, new DateTime(2026, 1, 10))
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _handler.Handle(
            new GetTransactionSummaryQuery(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)),
            CancellationToken.None);

        // Assert
        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(500m, result.TotalExpenses);
        Assert.Equal(-500m, result.NetAmount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetTransactionSummary_RespectsDateRange()
    {
        // Arrange — repository returns only Jan transactions (spec filters)
        var janTransactions = new List<Transaction>
        {
            CreateTransaction(-100m, new DateTime(2026, 1, 15))
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(janTransactions);

        // Act
        var result = await _handler.Handle(
            new GetTransactionSummaryQuery(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)),
            CancellationToken.None);

        // Assert
        Assert.Equal(100m, result.TotalExpenses);
        Assert.Equal(1, result.TransactionCount);
    }
}
