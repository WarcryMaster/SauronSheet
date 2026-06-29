namespace SauronSheet.Application.Tests.Features.Analytics.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

using SauronSheet.Application.Features.Analytics.Classification;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Application.Tests.Common;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Integration tests for GetAnnualDashboardQueryHandler.
/// Uses real AnnualClassificationEngine and mocked repositories.
/// </summary>
public class GetAnnualDashboardQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ISubcategoryRepository> _subcategoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly GetAnnualDashboardQueryHandler _handler;

    public GetAnnualDashboardQueryHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns("user-1");
        _handler = new GetAnnualDashboardQueryHandler(
            _transactionRepoMock.Object,
            _subcategoryRepoMock.Object,
            _userContextMock.Object,
            new AnnualClassificationEngine());
    }

    private static Transaction CreateTransaction(
        decimal amount,
        DateTime date,
        SubcategoryId? subcategoryId = null,
        string description = "Test")
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(amount, "EUR"),
            date,
            description,
            subcategoryId: subcategoryId);
    }

    private static Subcategory CreateSubcategory(SubcategoryId id, string name)
    {
        return TestSubcategoryFactory.CreateUserSubcategory(
            subcategoryId: id,
            userId: new UserId("user-1"),
            name: name);
    }

    private void SetupYearRange(DateTime minDate, DateTime maxDate)
    {
        _transactionRepoMock
            .Setup(x => x.GetDateRangeAsync(It.IsAny<UserId>()))
            .ReturnsAsync((minDate, maxDate));
    }

    private void SetupTransactions(IReadOnlyList<Transaction> transactions)
    {
        _transactionRepoMock
            .Setup(x => x.GetByUserIdAndYearRangeAsync(
                It.IsAny<UserId>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((UserId userId, int fromYear, int toYear) =>
            {
                return transactions
                    .Where(t => t.Date.Year >= fromYear && t.Date.Year <= toYear)
                    .ToList();
            });
    }

    private void SetupSubcategories(List<Subcategory> subcategories)
    {
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(subcategories);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_NoTransactions_ReturnsEmptyDashboard()
    {
        // Arrange
        SetupTransactions(new List<Transaction>());
        SetupSubcategories(new List<Subcategory>());

        // Act
        GetAnnualDashboardResultDto result = await _handler.Handle(
            new GetAnnualDashboardQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.False(result.HasData);
        Assert.Empty(result.Rows);
        Assert.Null(result.Ratios);
        Assert.Null(result.HealthScore);
        Assert.NotNull(result.ExecutiveSummary);
        Assert.Equal(2026, result.ExecutiveSummary.Year);
        Assert.Equal(0m, result.ExecutiveSummary.Income);
        Assert.Equal(0m, result.ExecutiveSummary.Expense);
        Assert.Contains("Sin datos", result.SmartSummary);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SingleYear_ReturnsDashboardWithCorrectData()
    {
        // Arrange
        SubcategoryId salaryId = SubcategoryId.New();

        List<Transaction> transactions = new()
        {
            CreateTransaction(3000m, new DateTime(2026, 1, 15), salaryId, "Salary Jan"),
            CreateTransaction(3000m, new DateTime(2026, 2, 15), salaryId, "Salary Feb"),
            CreateTransaction(3000m, new DateTime(2026, 3, 15), salaryId, "Salary Mar"),
            CreateTransaction(-500m, new DateTime(2026, 1, 10), null, "Rent Jan"),
            CreateTransaction(-500m, new DateTime(2026, 2, 10), null, "Rent Feb"),
            CreateTransaction(-500m, new DateTime(2026, 3, 10), null, "Rent Mar"),
        };

        SetupYearRange(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        SetupTransactions(transactions);
        SetupSubcategories(new List<Subcategory>
        {
            CreateSubcategory(salaryId, "Salary"),
        });

        // Act
        GetAnnualDashboardResultDto result = await _handler.Handle(
            new GetAnnualDashboardQuery(2026),
            CancellationToken.None);

        // Assert — Executive Summary
        Assert.True(result.HasData);
        Assert.NotNull(result.ExecutiveSummary);
        Assert.Equal(9000m, result.ExecutiveSummary.Income);
        Assert.Equal(1500m, result.ExecutiveSummary.Expense);
        Assert.Equal(7500m, result.ExecutiveSummary.Net);

        // Assert — Ratios (6 non-zero transactions: 3 income + 3 expense)
        Assert.NotNull(result.Ratios);
        Assert.Equal(6, result.Ratios.TransactionCount);
        Assert.NotNull(result.Ratios.SavingsRate);
        Assert.Equal(83.33m, Math.Round(result.Ratios.SavingsRate!.Value, 2));

        // Assert — Health Score
        Assert.NotNull(result.HealthScore);
        Assert.NotNull(result.HealthScore.Total);
        Assert.True(result.HealthScore.Total > 0m);

        // Assert — Smart Summary
        Assert.False(string.IsNullOrEmpty(result.SmartSummary));
        Assert.DoesNotContain("Sin datos", result.SmartSummary);

        // Assert — AnalysisSummary (existing format)
        Assert.Equal(9000m, result.AnalysisSummary.IncomeTotal);
        Assert.Equal(1500m, result.AnalysisSummary.ExpenseTotal);
        Assert.Equal(3, result.AnalysisSummary.MonthsWithData); // 3 distinct months
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_WithPreviousYear_ComputesYoYChanges()
    {
        // Arrange
        SubcategoryId salaryId = SubcategoryId.New();

        List<Transaction> transactions = new()
        {
            // Current year
            CreateTransaction(4000m, new DateTime(2026, 1, 15), salaryId, "Salary"),
            CreateTransaction(-600m, new DateTime(2026, 1, 10), null, "Rent"),
            // Previous year
            CreateTransaction(3000m, new DateTime(2025, 1, 15), salaryId, "Salary PY"),
            CreateTransaction(-500m, new DateTime(2025, 1, 10), null, "Rent PY"),
        };

        SetupYearRange(new DateTime(2025, 1, 1), new DateTime(2026, 12, 31));
        SetupTransactions(transactions);
        SetupSubcategories(new List<Subcategory>
        {
            CreateSubcategory(salaryId, "Salary"),
        });

        // Act
        GetAnnualDashboardResultDto result = await _handler.Handle(
            new GetAnnualDashboardQuery(2026),
            CancellationToken.None);

        // Assert — YoY
        Assert.True(result.HasData);
        Assert.NotNull(result.ExecutiveSummary);
        Assert.True(result.ExecutiveSummary.HasPreviousYear);

        // Income: 4000 - 3000 = 1000 abs, 33.33%
        Assert.Equal(1000m, result.ExecutiveSummary.IncomeChangeAbs);
        Assert.True(result.ExecutiveSummary.IncomeChangePct > 0m);

        // Expense: -600 vs -500 = 100 abs increase
        Assert.Equal(100m, result.ExecutiveSummary.ExpenseChangeAbs);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_MultipleYears_ComputesYearRank()
    {
        // Arrange
        SubcategoryId salaryId = SubcategoryId.New();

        List<Transaction> transactions = new()
        {
            // 2024: 2000 income, 500 expense → net = 1500
            CreateTransaction(2000m, new DateTime(2024, 6, 15), salaryId, "Salary 24"),
            CreateTransaction(-500m, new DateTime(2024, 6, 10), null, "Rent 24"),
            // 2025: 3000 income, 500 expense → net = 2500 (best)
            CreateTransaction(3000m, new DateTime(2025, 6, 15), salaryId, "Salary 25"),
            CreateTransaction(-500m, new DateTime(2025, 6, 10), null, "Rent 25"),
            // 2026: 1000 income, 500 expense → net = 500 (worst)
            CreateTransaction(1000m, new DateTime(2026, 6, 15), salaryId, "Salary 26"),
            CreateTransaction(-500m, new DateTime(2026, 6, 10), null, "Rent 26"),
        };

        SetupYearRange(new DateTime(2024, 1, 1), new DateTime(2026, 12, 31));
        SetupTransactions(transactions);
        SetupSubcategories(new List<Subcategory>
        {
            CreateSubcategory(salaryId, "Salary"),
        });

        // Act — request 2026 (rank should be 3 = worst)
        GetAnnualDashboardResultDto result = await _handler.Handle(
            new GetAnnualDashboardQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.NotNull(result.ExecutiveSummary);
        Assert.Equal(3, result.ExecutiveSummary.YearRank);
        Assert.Equal(3, result.ExecutiveSummary.TotalYears);
    }

    // ── T2 Handler Extension Tests (Task 5.6) ──

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_MultipleYears_ReturnsMultiYearComparisonData()
    {
        // Arrange
        SubcategoryId salaryId = SubcategoryId.New();

        List<Transaction> transactions = new()
        {
            // 2025: 3000 income, 500 expense
            CreateTransaction(3000m, new DateTime(2025, 6, 15), salaryId, "Salary 25"),
            CreateTransaction(-500m, new DateTime(2025, 6, 10), null, "Rent 25"),
            // 2026: 4000 income, 600 expense
            CreateTransaction(4000m, new DateTime(2026, 1, 15), salaryId, "Salary 26"),
            CreateTransaction(-600m, new DateTime(2026, 1, 10), null, "Rent 26"),
        };

        SetupYearRange(new DateTime(2025, 1, 1), new DateTime(2026, 12, 31));
        SetupTransactions(transactions);
        SetupSubcategories(new List<Subcategory>
        {
            CreateSubcategory(salaryId, "Salary"),
        });

        // Act
        GetAnnualDashboardResultDto result = await _handler.Handle(
            new GetAnnualDashboardQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.NotNull(result.MultiYear);
        Assert.Equal(2, result.MultiYear.Years.Count);
        Assert.Contains(2025, result.MultiYear.Years);
        Assert.Contains(2026, result.MultiYear.Years);
        Assert.Equal(2026, result.MultiYear.HighlightYear);

        // 2025: 3000 income, 500 expense, 2500 savings
        Assert.Equal(3000m, result.MultiYear.Incomes[0]);
        Assert.Equal(500m, result.MultiYear.Expenses[0]);
        Assert.Equal(2500m, result.MultiYear.Savings[0]);

        // 2026: 4000 income, 600 expense, 3400 savings
        Assert.Equal(4000m, result.MultiYear.Incomes[1]);
        Assert.Equal(600m, result.MultiYear.Expenses[1]);
        Assert.Equal(3400m, result.MultiYear.Savings[1]);

        // Best year = 2026 (highest savings 3400 > 2500)
        Assert.Equal(2026, result.MultiYear.BestYear);
        Assert.Equal(2025, result.MultiYear.WorstYear);

        // Has previous year pointer (2025 is before selected 2026)
        Assert.NotNull(result.MultiYear.PreviousYearValue);
        Assert.Equal(3000m, result.MultiYear.PreviousYearValue.Income);
        Assert.Equal(500m, result.MultiYear.PreviousYearValue.Expense);

        // No next year
        Assert.Null(result.MultiYear.NextYearValue);

        // Averages
        Assert.NotNull(result.MultiYear.Average);
        Assert.Equal(3500m, result.MultiYear.Average.Income); // (3000+4000)/2
        Assert.Equal(550m, result.MultiYear.Average.Expense); // (500+600)/2
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_WithTransactions_ReturnsCategoryDistribution()
    {
        // Arrange
        SubcategoryId foodId = SubcategoryId.New();
        SubcategoryId transportId = SubcategoryId.New();

        List<Transaction> transactions = new()
        {
            CreateTransaction(3000m, new DateTime(2026, 1, 15), SubcategoryId.New(), "Salary"),
            CreateTransaction(-200m, new DateTime(2026, 1, 10), foodId, "Supermarket"),
            CreateTransaction(-150m, new DateTime(2026, 2, 10), transportId, "Bus pass"),
            CreateTransaction(-100m, new DateTime(2026, 3, 10), foodId, "Restaurant"),
        };

        SetupYearRange(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        SetupTransactions(transactions);
        SetupSubcategories(new List<Subcategory>
        {
            CreateSubcategory(foodId, "Food"),
            CreateSubcategory(transportId, "Transport"),
        });

        // Act
        GetAnnualDashboardResultDto result = await _handler.Handle(
            new GetAnnualDashboardQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.NotNull(result.Categories);
        Assert.NotEmpty(result.Categories);

        // Categories are expense-only, sorted by amount descending
        // Food: 200 + 100 = 300 (rank 1), Transport: 150 (rank 2)
        CategoryItemDto foodCategory = result.Categories.First(c => c.CategoryName == "Food");
        Assert.Equal(300m, foodCategory.Amount);
        Assert.Equal(1, foodCategory.Rank);
        Assert.Equal(67m, foodCategory.Percentage); // 300/450 * 100 ≈ 67

        CategoryItemDto transportCategory = result.Categories.First(c => c.CategoryName == "Transport");
        Assert.Equal(150m, transportCategory.Amount);
        Assert.Equal(2, transportCategory.Rank);
        Assert.Equal(33m, transportCategory.Percentage); // 150/450 * 100 ≈ 33
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_WithSufficientTransactions_ReturnsTimelineAndTopMovements()
    {
        // Arrange — transactions across multiple dates with varying amounts
        SubcategoryId salaryId = SubcategoryId.New();
        SubcategoryId foodId = SubcategoryId.New();

        List<Transaction> transactions = new()
        {
            CreateTransaction(5000m, new DateTime(2026, 6, 15), salaryId, "Salary"),      // highest income
            CreateTransaction(-2000m, new DateTime(2026, 1, 10), foodId, "Rent"),          // biggest expense
            CreateTransaction(-500m, new DateTime(2026, 3, 15), foodId, "Supermarket"),    // another expense
            CreateTransaction(200m, new DateTime(2026, 12, 20), null, "Gift"),            // last transaction
        };

        SetupYearRange(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        SetupTransactions(transactions);
        SetupSubcategories(new List<Subcategory>
        {
            CreateSubcategory(salaryId, "Salary"),
            CreateSubcategory(foodId, "Food"),
        });

        // Act
        GetAnnualDashboardResultDto result = await _handler.Handle(
            new GetAnnualDashboardQuery(2026),
            CancellationToken.None);

        // Assert — Timeline
        Assert.NotNull(result.Timeline);
        Assert.NotEmpty(result.Timeline);
        Assert.Contains(result.Timeline, e => e.Type == "highest-income");
        Assert.Contains(result.Timeline, e => e.Type == "biggest-expense");
        Assert.Contains(result.Timeline, e => e.Type == "last-transaction");

        // Highest income: 5000 (Salary)
        TimelineEventDto highestIncome = result.Timeline.First(e => e.Type == "highest-income");
        Assert.Equal(5000m, highestIncome.Amount);

        // Biggest expense: 2000 (Rent)
        TimelineEventDto biggestExpense = result.Timeline.First(e => e.Type == "biggest-expense");
        Assert.Equal(2000m, biggestExpense.Amount);

        // Assert — Top Expenses
        Assert.NotNull(result.TopExpenses);
        Assert.NotEmpty(result.TopExpenses);
        Assert.Contains(result.TopExpenses, t => t.Description == "Rent");
        Assert.Contains(result.TopExpenses, t => t.Description == "Supermarket");

        // Assert — Top Incomes
        Assert.NotNull(result.TopIncomes);
        Assert.NotEmpty(result.TopIncomes);
        Assert.Contains(result.TopIncomes, t => t.Description == "Salary");
        Assert.Contains(result.TopIncomes, t => t.Description == "Gift");

        // Assert — Most Frequent
        Assert.NotNull(result.MostFrequent);
        Assert.NotEmpty(result.MostFrequent);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_MultiYearData_ReturnsT3AdvancedSections()
    {
        // Arrange
        List<Transaction> transactions = new()
        {
            CreateTransaction(2000m, new DateTime(2024, 1, 10), null, "Salary 2024"),
            CreateTransaction(-900m, new DateTime(2024, 8, 10), null, "Food"),
            CreateTransaction(2200m, new DateTime(2025, 1, 10), null, "Salary 2025"),
            CreateTransaction(-950m, new DateTime(2025, 8, 10), null, "Food"),
            CreateTransaction(2600m, new DateTime(2026, 1, 10), null, "Salary 2026"),
            CreateTransaction(-1600m, new DateTime(2026, 8, 10), null, "Food"),
        };

        SetupYearRange(new DateTime(2024, 1, 1), new DateTime(2026, 12, 31));
        SetupTransactions(transactions);
        SetupSubcategories(new List<Subcategory>());

        // Act
        GetAnnualDashboardResultDto result = await _handler.Handle(
            new GetAnnualDashboardQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.NotNull(result.Anomalies);
        Assert.NotNull(result.Discoveries);
        Assert.NotNull(result.Achievements);
        Assert.NotNull(result.Trends);
        Assert.NotNull(result.Predictions);
        Assert.NotNull(result.HistoricalComparison);
        Assert.True(result.Predictions.HasEnoughData);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SingleYearData_ReturnsT3FallbackForPredictionsAndHistoricalComparison()
    {
        // Arrange
        List<Transaction> transactions = new()
        {
            CreateTransaction(2600m, new DateTime(2026, 1, 10), null, "Salary 2026"),
            CreateTransaction(-900m, new DateTime(2026, 8, 10), null, "Food"),
        };

        SetupYearRange(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        SetupTransactions(transactions);
        SetupSubcategories(new List<Subcategory>());

        // Act
        GetAnnualDashboardResultDto result = await _handler.Handle(
            new GetAnnualDashboardQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.NotNull(result.Predictions);
        Assert.False(result.Predictions.HasEnoughData);
        Assert.NotNull(result.HistoricalComparison);
        Assert.Contains("Need 2+", result.HistoricalComparison.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_LoadsTransactionsOnlyOnce_ForT1T2T3Composition()
    {
        // Arrange
        List<Transaction> transactions = new()
        {
            CreateTransaction(2000m, new DateTime(2025, 1, 10), null, "Salary 2025"),
            CreateTransaction(-900m, new DateTime(2025, 8, 10), null, "Food 2025"),
            CreateTransaction(2600m, new DateTime(2026, 1, 10), null, "Salary 2026"),
            CreateTransaction(-1500m, new DateTime(2026, 8, 10), null, "Food 2026"),
        };

        SetupYearRange(new DateTime(2025, 1, 1), new DateTime(2026, 12, 31));
        SetupTransactions(transactions);
        SetupSubcategories(new List<Subcategory>());

        // Act
        GetAnnualDashboardResultDto result = await _handler.Handle(
            new GetAnnualDashboardQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        _transactionRepoMock.Verify(
            x => x.GetByUserIdAndYearRangeAsync(It.IsAny<UserId>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
    }
}
