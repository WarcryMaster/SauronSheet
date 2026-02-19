using Moq;
using Xunit;
using SauronSheet.Application.Common;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Analytics.Queries;

/// <summary>
/// Tests for GetMonthlyTrendsQueryHandler.
/// Phase 4 (US3): Monthly spending trends (line chart).
/// </summary>
public class GetMonthlyTrendsQueryTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly GetMonthlyTrendsQueryHandler _handler;

    public GetMonthlyTrendsQueryTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns("user-1");
        _handler = new GetMonthlyTrendsQueryHandler(
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
    public async Task GetMonthlyTrends_FullYear_Returns12Entries()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CreateTransaction(-100m, new DateTime(2026, 1, 15)),
            CreateTransaction(-200m, new DateTime(2026, 3, 10)),
            CreateTransaction(-300m, new DateTime(2026, 6, 20)),
            CreateTransaction(-150m, new DateTime(2026, 12, 5))
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _handler.Handle(new GetMonthlyTrendsQuery(2026), CancellationToken.None);

        // Assert
        Assert.Equal(12, result.Count);
        Assert.Equal(100m, result[0].TotalExpenses);   // January
        Assert.Equal(0m, result[1].TotalExpenses);     // February (no data)
        Assert.Equal(200m, result[2].TotalExpenses);   // March
        Assert.Equal(300m, result[5].TotalExpenses);   // June
        Assert.Equal(150m, result[11].TotalExpenses);  // December
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetMonthlyTrends_NoTransactions_Returns12ZeroEntries()
    {
        // Arrange
        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());

        // Act
        var result = await _handler.Handle(new GetMonthlyTrendsQuery(2026), CancellationToken.None);

        // Assert
        Assert.Equal(12, result.Count);
        Assert.All(result, entry =>
        {
            Assert.Equal(0m, entry.TotalExpenses);
            Assert.Equal(0m, entry.TotalIncome);
            Assert.Equal(0m, entry.NetAmount);
            Assert.Equal(0, entry.TransactionCount);
        });
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetMonthlyTrends_SeparatesIncomeAndExpenses()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CreateTransaction(500m, new DateTime(2026, 1, 5)),    // income
            CreateTransaction(-300m, new DateTime(2026, 1, 15)),  // expense
            CreateTransaction(-100m, new DateTime(2026, 1, 25))   // expense
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _handler.Handle(new GetMonthlyTrendsQuery(2026), CancellationToken.None);

        // Assert — January
        Assert.Equal(500m, result[0].TotalIncome);
        Assert.Equal(400m, result[0].TotalExpenses);
        Assert.Equal(100m, result[0].NetAmount);
        Assert.Equal(3, result[0].TransactionCount);
    }
}
