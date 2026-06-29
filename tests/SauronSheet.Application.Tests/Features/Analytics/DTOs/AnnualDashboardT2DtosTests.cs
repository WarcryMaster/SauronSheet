namespace SauronSheet.Application.Tests.Features.Analytics.DTOs;

using System.Collections.Generic;
using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Tests for the T2 (Multi-Year) DTOs (PR 2).
/// Strict TDD: RED — Tests first, then implement DTOs.
/// Covers Tasks 1.1 through 1.6.
/// </summary>
[Trait("Category", "Application")]
public class AnnualDashboardT2DtosTests
{
    // ── Task 1.1: AnnualDashboardMultiYearDto ──

    [Fact]
    public void MultiYearDto_ConstructedWithValues_PropertiesMatch()
    {
        // Arrange
        IReadOnlyList<int> years = new List<int> { 2024, 2025, 2026 }.AsReadOnly();
        IReadOnlyList<decimal> incomes = new List<decimal> { 40000m, 45000m, 50000m }.AsReadOnly();
        IReadOnlyList<decimal> expenses = new List<decimal> { 25000m, 28000m, 30000m }.AsReadOnly();
        IReadOnlyList<decimal> savings = new List<decimal> { 15000m, 17000m, 20000m }.AsReadOnly();
        IReadOnlyList<decimal> balances = new List<decimal> { 15000m, 17000m, 20000m }.AsReadOnly();

        // Act
        AnnualDashboardMultiYearDto dto = new(
            Years: years,
            Incomes: incomes,
            Expenses: expenses,
            Savings: savings,
            Balances: balances,
            HighlightYear: 2026,
            PreviousYearValue: new MultiYearComparisonDto(45000m, 28000m, 17000m, 17000m),
            NextYearValue: null,
            Average: new MultiYearComparisonDto(45000m, 27666.67m, 17333.33m, 17333.33m),
            BestYear: 2026,
            WorstYear: 2024);

        // Assert
        Assert.Equal(3, dto.Years.Count);
        Assert.Equal(2026, dto.HighlightYear);
        Assert.NotNull(dto.PreviousYearValue);
        Assert.Null(dto.NextYearValue);
        Assert.NotNull(dto.Average);
        Assert.Equal(2026, dto.BestYear);
        Assert.Equal(2024, dto.WorstYear);
        Assert.Equal(45000m, dto.PreviousYearValue!.Income);
        Assert.Equal(17000m, dto.PreviousYearValue!.Savings);
        Assert.Equal(45000m, dto.Average!.Income);
        Assert.Equal(17333.33m, dto.Average!.Savings);
    }

    [Fact]
    public void MultiYearDto_SingleYear_AverageIsSameYear()
    {
        // Arrange
        IReadOnlyList<int> years = new List<int> { 2026 }.AsReadOnly();
        IReadOnlyList<decimal> incomes = new List<decimal> { 50000m }.AsReadOnly();
        IReadOnlyList<decimal> expenses = new List<decimal> { 30000m }.AsReadOnly();
        IReadOnlyList<decimal> savings = new List<decimal> { 20000m }.AsReadOnly();
        IReadOnlyList<decimal> balances = new List<decimal> { 20000m }.AsReadOnly();

        // Act
        AnnualDashboardMultiYearDto dto = new(
            Years: years,
            Incomes: incomes,
            Expenses: expenses,
            Savings: savings,
            Balances: balances,
            HighlightYear: 2026,
            PreviousYearValue: null,
            NextYearValue: null,
            Average: new MultiYearComparisonDto(50000m, 30000m, 20000m, 20000m),
            BestYear: 2026,
            WorstYear: 2026);

        // Assert
        Assert.Single(dto.Years);
        Assert.Null(dto.PreviousYearValue);
        Assert.Null(dto.NextYearValue);
        Assert.Equal(2026, dto.BestYear);
        Assert.Equal(2026, dto.WorstYear);
        Assert.Equal(50000m, dto.Average!.Income);
    }

    // ── Task 1.2: AnnualDashboardMonthlyDto ──

    [Fact]
    public void MonthlyDto_ConstructedWith12Months_PropertiesMatch()
    {
        // Arrange
        IReadOnlyList<decimal> incomes = new List<decimal>
            { 4000, 4000, 4000, 4000, 4000, 4000, 4000, 4000, 4000, 4000, 4000, 4000 }.AsReadOnly();
        IReadOnlyList<decimal> expenses = new List<decimal>
            { 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2500 }.AsReadOnly();
        IReadOnlyList<decimal> savings = new List<decimal>
            { 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 1500 }.AsReadOnly();

        // Act
        AnnualDashboardMonthlyDto dto = new(
            Incomes: incomes,
            Expenses: expenses,
            Savings: savings,
            PreviousYearAverageIncome: 3800m,
            PreviousYearAverageExpense: 2100m,
            HistoricalAverageIncome: 3900m,
            HistoricalAverageExpense: 2050m,
            BestIncomeMonth: 0,  // January
            BestExpenseMonth: 11, // December (lowest expense)
            WorstIncomeMonth: null,
            WorstExpenseMonth: 11); // December (highest expense)

        // Assert
        Assert.Equal(12, dto.Incomes.Count);
        Assert.Equal(12, dto.Expenses.Count);
        Assert.Equal(12, dto.Savings.Count);
        Assert.Equal(4000m, dto.Incomes[0]);
        Assert.Equal(2500m, dto.Expenses[11]);
        Assert.Equal(1500m, dto.Savings[11]);
        Assert.Equal(3800m, dto.PreviousYearAverageIncome);
        Assert.Equal(2100m, dto.PreviousYearAverageExpense);
        Assert.Equal(3900m, dto.HistoricalAverageIncome);
        Assert.Equal(2050m, dto.HistoricalAverageExpense);
        Assert.Equal(0, dto.BestIncomeMonth);
        Assert.Equal(11, dto.BestExpenseMonth);
        Assert.Null(dto.WorstIncomeMonth);
        Assert.Equal(11, dto.WorstExpenseMonth);
    }

    [Fact]
    public void MonthlyDto_AllZeros_NullWorstBest()
    {
        // Arrange
        IReadOnlyList<decimal> zeros = new List<decimal>
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }.AsReadOnly();

        // Act
        AnnualDashboardMonthlyDto dto = new(
            Incomes: zeros, Expenses: zeros, Savings: zeros,
            PreviousYearAverageIncome: null,
            PreviousYearAverageExpense: null,
            HistoricalAverageIncome: null,
            HistoricalAverageExpense: null,
            BestIncomeMonth: null,
            BestExpenseMonth: null,
            WorstIncomeMonth: null,
            WorstExpenseMonth: null);

        // Assert
        Assert.Equal(12, dto.Incomes.Count);
        Assert.Null(dto.PreviousYearAverageIncome);
        Assert.Null(dto.HistoricalAverageIncome);
        Assert.Null(dto.BestIncomeMonth);
        Assert.Null(dto.BestExpenseMonth);
    }

    // ── Task 1.3: CategoryItemDto ──

    [Fact]
    public void CategoryItemDto_ConstructedWithValues_PropertiesMatch()
    {
        // Arrange & Act
        CategoryItemDto dto = new(
            CategoryName: "Supermarket",
            Amount: 12000m,
            Percentage: 40m,
            Rank: 1,
            YoYChangeAbs: 1500m,
            YoYChangePct: 14.29m,
            Trend: "up",
            IsNewThisYear: false);

        // Assert
        Assert.Equal("Supermarket", dto.CategoryName);
        Assert.Equal(12000m, dto.Amount);
        Assert.Equal(40m, dto.Percentage);
        Assert.Equal(1, dto.Rank);
        Assert.Equal(1500m, dto.YoYChangeAbs);
        Assert.Equal(14.29m, dto.YoYChangePct);
        Assert.Equal("up", dto.Trend);
        Assert.False(dto.IsNewThisYear);
    }

    [Fact]
    public void CategoryItemDto_NewThisYear_IsNewTrue()
    {
        // Arrange & Act
        CategoryItemDto dto = new(
            CategoryName: "New Subscription",
            Amount: 1200m,
            Percentage: 5m,
            Rank: 5,
            YoYChangeAbs: null,
            YoYChangePct: null,
            Trend: "new",
            IsNewThisYear: true);

        // Assert
        Assert.Equal("New Subscription", dto.CategoryName);
        Assert.True(dto.IsNewThisYear);
        Assert.Null(dto.YoYChangeAbs);
        Assert.Equal("new", dto.Trend);
    }

    // ── Task 1.4: CategoryComparisonTableDto ──

    [Fact]
    public void CategoryComparisonTableDto_ConstructedWithRows_SortsByDiffDesc()
    {
        // Arrange
        IReadOnlyList<CategoryComparisonRowDto> rows = new List<CategoryComparisonRowDto>
        {
            new("Rent", 12000m, 12500m, null, 500m, 4.17m, "up"),
            new("Supermarket", 6000m, 7200m, 7500m, 1200m, 20m, "up"),
            new("Transport", 3000m, 2800m, 2900m, -200m, -6.67m, "down"),
        }.AsReadOnly();

        // Act
        CategoryComparisonTableDto dto = new(Rows: rows);

        // Assert
        Assert.Equal(3, dto.Rows.Count);
    }

    [Fact]
    public void CategoryComparisonRowDto_NoNextYear_NextIsNull()
    {
        // Arrange & Act
        CategoryComparisonRowDto dto = new(
            CategoryName: "Rent",
            PreviousYearAmount: 12000m,
            SelectedYearAmount: 12500m,
            NextYearAmount: null,
            DiffAbs: 500m,
            DiffPct: 4.17m,
            Trend: "up");

        // Assert
        Assert.Equal(12500m, dto.SelectedYearAmount);
        Assert.Null(dto.NextYearAmount);
        Assert.Equal(500m, dto.DiffAbs);
        Assert.Equal(4.17m, dto.DiffPct);
    }

    // ── Task 1.5: TimelineEventDto ──

    [Fact]
    public void TimelineEventDto_ConstructedWithValues_PropertiesMatch()
    {
        // Arrange & Act
        TimelineEventDto dto = new(
            Type: "highest-income",
            Label: "Highest Income",
            Description: "Salary payment - €4,000",
            Date: "2026-01-15",
            Amount: 4000m,
            Icon: "arrow-up");

        // Assert
        Assert.Equal("highest-income", dto.Type);
        Assert.Equal("Highest Income", dto.Label);
        Assert.Equal("2026-01-15", dto.Date);
        Assert.Equal(4000m, dto.Amount);
        Assert.Equal("arrow-up", dto.Icon);
    }

    [Fact]
    public void TimelineEventDto_ExpenseEvent_SavingsRecord_PropertiesMatch()
    {
        // Arrange & Act
        TimelineEventDto dto = new(
            Type: "biggest-expense",
            Label: "Biggest Expense",
            Description: "Car repair - €2,500",
            Date: "2026-03-10",
            Amount: 2500m,
            Icon: "arrow-down");

        // Assert
        Assert.Equal("biggest-expense", dto.Type);
        Assert.Equal(2500m, dto.Amount);
        Assert.Equal("arrow-down", dto.Icon);
    }

    // ── Task 1.6: TopMovementDto ──

    [Fact]
    public void TopMovementDto_ConstructedWithValues_PropertiesMatch()
    {
        // Arrange & Act
        TopMovementDto dto = new(
            Description: "Salary January",
            Amount: 4000m,
            Date: "2026-01-15",
            Category: "Salary",
            Type: "income",
            TransactionId: "trx-001");

        // Assert
        Assert.Equal("Salary January", dto.Description);
        Assert.Equal(4000m, dto.Amount);
        Assert.Equal("income", dto.Type);
        Assert.Equal("trx-001", dto.TransactionId);
    }

    [Fact]
    public void TopMovementDto_FrequentType_NoTransactionId()
    {
        // Arrange & Act
        TopMovementDto dto = new(
            Description: "Coffee shop",
            Amount: 4.50m,
            Date: "2026-02-10",
            Category: "Cafeterias",
            Type: "frequent",
            TransactionId: null);

        // Assert
        Assert.Equal("Coffee shop", dto.Description);
        Assert.Equal("frequent", dto.Type);
        Assert.Null(dto.TransactionId);
    }
}
