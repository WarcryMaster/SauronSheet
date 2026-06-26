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
/// Tests for GetMonthlyTrendsQueryHandler.
/// Updated for date-range signature (FromDate, ToDate) and Year field.
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
    public async Task Handle_FullYearRange_Returns12Entries()
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
        var result = await _handler.Handle(
            new GetMonthlyTrendsQuery(
                new DateTime(2026, 1, 1),
                new DateTime(2026, 12, 31)),
            CancellationToken.None);

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
    public async Task Handle_FullYearRange_YearFieldPopulated()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CreateTransaction(-100m, new DateTime(2026, 3, 10))
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _handler.Handle(
            new GetMonthlyTrendsQuery(
                new DateTime(2026, 1, 1),
                new DateTime(2026, 12, 31)),
            CancellationToken.None);

        // Assert — Year field populated for all entries
        Assert.All(result, entry => Assert.Equal(2026, entry.Year));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_NoTransactions_ReturnsZeroEntriesForRange()
    {
        // Arrange
        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());

        // Act
        var result = await _handler.Handle(
            new GetMonthlyTrendsQuery(
                new DateTime(2026, 4, 1),
                new DateTime(2026, 6, 30)),
            CancellationToken.None);

        // Assert — 3 months in range (April, May, June), all zeros
        Assert.Equal(3, result.Count);
        Assert.All(result, entry =>
        {
            Assert.Equal(0m, entry.TotalExpenses);
            Assert.Equal(0m, entry.TotalIncome);
            Assert.Equal(0m, entry.NetAmount);
            Assert.Equal(0, entry.TransactionCount);
        });
        Assert.Equal(2026, result[0].Year);
        Assert.Equal(4, result[0].Month);
        Assert.Equal(5, result[1].Month);
        Assert.Equal(6, result[2].Month);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_PartialRange_PadsMissingMonthsWithZeros()
    {
        // Arrange — expenses only in April and June, May is missing
        var transactions = new List<Transaction>
        {
            CreateTransaction(-100m, new DateTime(2026, 4, 10)),
            CreateTransaction(-200m, new DateTime(2026, 6, 15))
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _handler.Handle(
            new GetMonthlyTrendsQuery(
                new DateTime(2026, 4, 1),
                new DateTime(2026, 6, 30)),
            CancellationToken.None);

        // Assert — 3 entries: April (100), May (0), June (200)
        Assert.Equal(3, result.Count);
        Assert.Equal(4, result[0].Month);
        Assert.Equal(100m, result[0].TotalExpenses);
        Assert.Equal(5, result[1].Month);
        Assert.Equal(0m, result[1].TotalExpenses);  // May padded with zero
        Assert.Equal(6, result[2].Month);
        Assert.Equal(200m, result[2].TotalExpenses);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SeparatesIncomeAndExpenses()
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
        var result = await _handler.Handle(
            new GetMonthlyTrendsQuery(
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31)),
            CancellationToken.None);

        // Assert — single month
        Assert.Single(result);
        Assert.Equal(500m, result[0].TotalIncome);
        Assert.Equal(400m, result[0].TotalExpenses);
        Assert.Equal(100m, result[0].NetAmount);
        Assert.Equal(3, result[0].TransactionCount);
        Assert.Equal(2026, result[0].Year);
    }
}
