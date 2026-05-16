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
/// Tests for GetYearlyComparisonQueryHandler.
/// Phase 4 (US4): Year-over-year income and expenses comparison (bar chart).
/// </summary>
public class GetYearlyComparisonQueryTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly GetYearlyComparisonQueryHandler _handler;

    public GetYearlyComparisonQueryTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns("user-1");
        _handler = new GetYearlyComparisonQueryHandler(
            _transactionRepoMock.Object,
            _userContextMock.Object);
    }

    private static Transaction CreateExpense(decimal amount, DateTime date)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(amount, "EUR"),
            date,
            "Test expense");
    }

    private static Transaction CreateIncome(decimal amount, DateTime date)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(amount, "EUR"),
            date,
            "Test income");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetYearlyComparison_TwoYears_ReturnsMonthlyComparison()
    {
        // Arrange
        var year1Transactions = new List<Transaction>
        {
            CreateExpense(-100m, new DateTime(2025, 1, 15)),
            CreateExpense(-200m, new DateTime(2025, 2, 10)),
            CreateIncome(500m, new DateTime(2025, 1, 1)),
            CreateIncome(600m, new DateTime(2025, 2, 1))
        };
        var year2Transactions = new List<Transaction>
        {
            CreateExpense(-150m, new DateTime(2026, 1, 15)),
            CreateExpense(-180m, new DateTime(2026, 2, 10)),
            CreateIncome(550m, new DateTime(2026, 1, 1)),
            CreateIncome(620m, new DateTime(2026, 2, 1))
        };

        _transactionRepoMock
            .SetupSequence(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(year1Transactions)
            .ReturnsAsync(year2Transactions);

        // Act
        var result = await _handler.Handle(
            new GetYearlyComparisonQuery(2025, 2026), CancellationToken.None);

        // Assert
        Assert.Equal(12, result.Count);

        // January
        Assert.Equal(500m, result[0].Year1Income);
        Assert.Equal(100m, result[0].Year1Expenses);
        Assert.Equal(550m, result[0].Year2Income);
        Assert.Equal(150m, result[0].Year2Expenses);

        // February
        Assert.Equal(600m, result[1].Year1Income);
        Assert.Equal(200m, result[1].Year1Expenses);
        Assert.Equal(620m, result[1].Year2Income);
        Assert.Equal(180m, result[1].Year2Expenses);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetYearlyComparison_NoDataForOneYear_ReturnsZeros()
    {
        // Arrange — Year 1 empty
        var year2Transactions = new List<Transaction>
        {
            CreateExpense(-150m, new DateTime(2026, 1, 15)),
            CreateIncome(400m, new DateTime(2026, 1, 1))
        };

        _transactionRepoMock
            .SetupSequence(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>())
            .ReturnsAsync(year2Transactions);

        // Act
        var result = await _handler.Handle(
            new GetYearlyComparisonQuery(2024, 2026), CancellationToken.None);

        // Assert
        Assert.Equal(12, result.Count);
        Assert.Equal(0m, result[0].Year1Income);
        Assert.Equal(0m, result[0].Year1Expenses);
        Assert.Equal(400m, result[0].Year2Income);
        Assert.Equal(150m, result[0].Year2Expenses);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetYearlyComparison_MixedIncomeAndExpenses_SeparatesCorrectly()
    {
        // Arrange
        var year1Transactions = new List<Transaction>
        {
            CreateExpense(-100m, new DateTime(2025, 3, 15)),
            CreateIncome(300m, new DateTime(2025, 3, 1))
        };
        var year2Transactions = new List<Transaction>
        {
            CreateExpense(-50m, new DateTime(2026, 3, 15)),
            CreateIncome(350m, new DateTime(2026, 3, 1))
        };

        _transactionRepoMock
            .SetupSequence(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(year1Transactions)
            .ReturnsAsync(year2Transactions);

        // Act
        var result = await _handler.Handle(
            new GetYearlyComparisonQuery(2025, 2026), CancellationToken.None);

        // Assert — March (index 2)
        Assert.Equal(300m, result[2].Year1Income);
        Assert.Equal(100m, result[2].Year1Expenses);
        Assert.Equal(350m, result[2].Year2Income);
        Assert.Equal(50m, result[2].Year2Expenses);
    }
}
