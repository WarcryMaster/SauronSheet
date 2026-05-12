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
/// Phase 4 (US4): Year-over-year spending comparison (bar chart).
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

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetYearlyComparison_TwoYears_ReturnsMonthlyComparison()
    {
        // Arrange
        var year1Transactions = new List<Transaction>
        {
            CreateExpense(-100m, new DateTime(2025, 1, 15)),
            CreateExpense(-200m, new DateTime(2025, 2, 10))
        };
        var year2Transactions = new List<Transaction>
        {
            CreateExpense(-150m, new DateTime(2026, 1, 15)),
            CreateExpense(-180m, new DateTime(2026, 2, 10))
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
        Assert.Equal(100m, result[0].Year1Amount);  // Jan 2025
        Assert.Equal(150m, result[0].Year2Amount);  // Jan 2026
        Assert.Equal(50m, result[0].Difference);    // increase
        Assert.Equal(200m, result[1].Year1Amount);  // Feb 2025
        Assert.Equal(180m, result[1].Year2Amount);  // Feb 2026
        Assert.Equal(-20m, result[1].Difference);   // decrease
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetYearlyComparison_NoDataForOneYear_ReturnsZeros()
    {
        // Arrange — Year 1 empty
        _transactionRepoMock
            .SetupSequence(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>())
            .ReturnsAsync(new List<Transaction>
            {
                CreateExpense(-150m, new DateTime(2026, 1, 15))
            });

        // Act
        var result = await _handler.Handle(
            new GetYearlyComparisonQuery(2024, 2026), CancellationToken.None);

        // Assert
        Assert.Equal(12, result.Count);
        Assert.Equal(0m, result[0].Year1Amount);   // No data for year 1
        Assert.Equal(150m, result[0].Year2Amount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetYearlyComparison_PercentageChange_ZeroDivision()
    {
        // Arrange — Year 1 has 0 in January
        _transactionRepoMock
            .SetupSequence(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>()) // Year 1 empty
            .ReturnsAsync(new List<Transaction>
            {
                CreateExpense(-150m, new DateTime(2026, 1, 15))
            });

        // Act
        var result = await _handler.Handle(
            new GetYearlyComparisonQuery(2025, 2026), CancellationToken.None);

        // Assert — PercentageChange should be null when year1 = 0
        Assert.Null(result[0].PercentageChange);
    }
}
