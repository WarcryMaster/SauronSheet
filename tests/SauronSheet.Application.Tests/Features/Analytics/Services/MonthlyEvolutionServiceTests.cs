namespace SauronSheet.Application.Tests.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Services;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Tests for MonthlyEvolutionService (REQ-004).
/// Strict TDD: RED — Tests first, then implement.
/// Task 2.2: Pure service that computes monthly income/expense/savings.
/// </summary>
[Trait("Category", "Application")]
public class MonthlyEvolutionServiceTests
{
    private static readonly UserId TestUserId = new("test-user");

    private static Transaction CreateTransaction(decimal amount, DateTime date, SubcategoryId? subcategoryId = null)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            TestUserId,
            new Money(amount, "EUR"),
            date,
            "Test",
            subcategoryId: subcategoryId);
    }

    [Fact]
    public void Compute_TwelveMonthsComplete_ReturnsMonthlyArrays()
    {
        // Arrange: one transaction per month
        SubcategoryId salaryId = SubcategoryId.New();
        List<Transaction> transactions = new();
        for (int m = 0; m < 12; m++)
        {
            transactions.Add(CreateTransaction(3000m, new DateTime(2026, m + 1, 15), salaryId));
            transactions.Add(CreateTransaction(-1500m, new DateTime(2026, m + 1, 10), null));
        }

        // Act
        AnnualDashboardMonthlyDto result = MonthlyEvolutionService.Compute(transactions, 2026, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(12, result.Incomes.Count);
        Assert.Equal(12, result.Expenses.Count);
        Assert.Equal(12, result.Savings.Count);

        // Every month: income=3000, expense=1500, savings=1500
        for (int m = 0; m < 12; m++)
        {
            Assert.Equal(3000m, result.Incomes[m]);
            Assert.Equal(1500m, result.Expenses[m]);
            Assert.Equal(1500m, result.Savings[m]);
        }

        // All months same → best is first (0), worst is last (11)
        Assert.Equal(0, result.BestIncomeMonth);
        Assert.Equal(0, result.BestExpenseMonth);
        Assert.Equal(11, result.WorstIncomeMonth);
        Assert.Equal(11, result.WorstExpenseMonth);

        // No historical data → averages are null
        Assert.Null(result.PreviousYearAverageIncome);
        Assert.Null(result.HistoricalAverageIncome);
    }

    [Fact]
    public void Compute_EmptyMonths_ReturnsZerosForMissing()
    {
        // Arrange: only 3 months of data
        List<Transaction> transactions = new()
        {
            CreateTransaction(5000m, new DateTime(2026, 1, 15)),
            CreateTransaction(-2000m, new DateTime(2026, 1, 10)),
            CreateTransaction(4000m, new DateTime(2026, 3, 15)),
            CreateTransaction(-1000m, new DateTime(2026, 3, 10)),
        };

        // Act
        AnnualDashboardMonthlyDto result = MonthlyEvolutionService.Compute(transactions, 2026, null);

        // Assert
        Assert.Equal(12, result.Incomes.Count);
        Assert.Equal(5000m, result.Incomes[0]);  // January
        Assert.Equal(0m, result.Incomes[1]);     // February
        Assert.Equal(4000m, result.Incomes[2]);  // March
        Assert.Equal(0m, result.Incomes[3]);     // April onwards

        // Best income month: January (5000)
        Assert.Equal(0, result.BestIncomeMonth);

        // Best expense month: March (1000)
        Assert.Equal(2, result.BestExpenseMonth);

        // Worst expense month: January (2000)
        Assert.Equal(0, result.WorstExpenseMonth);
    }

    [Fact]
    public void Compute_WithHistoricalData_ComputesOverlays()
    {
        // Arrange: current year + all previous years
        List<Transaction> currentYear = new()
        {
            CreateTransaction(3000m, new DateTime(2026, 1, 15)),
            CreateTransaction(-1500m, new DateTime(2026, 1, 10)),
        };

        Dictionary<int, List<Transaction>>? allYearsTransactions = new()
        {
            { 2024, new List<Transaction>
                {
                    CreateTransaction(2800m, new DateTime(2024, 1, 15)),
                    CreateTransaction(-1400m, new DateTime(2024, 1, 10)),
                }
            },
            { 2025, new List<Transaction>
                {
                    CreateTransaction(2900m, new DateTime(2025, 1, 15)),
                    CreateTransaction(-1450m, new DateTime(2025, 1, 10)),
                }
            },
            { 2026, currentYear },
        };

        // Act
        AnnualDashboardMonthlyDto result = MonthlyEvolutionService.Compute(currentYear, 2026, allYearsTransactions);

        // Assert — previous year (2025) averages
        Assert.NotNull(result.PreviousYearAverageIncome);
        Assert.NotNull(result.PreviousYearAverageExpense);

        // Previous year: income = 2900/12 ≈ 241.67 monthly avg
        Assert.Equal(241.67m, Math.Round(result.PreviousYearAverageIncome!.Value, 2));

        // Historical avg: (2800+2900)/2/12 ≈ 237.50
        Assert.NotNull(result.HistoricalAverageIncome);
        Assert.Equal(237.50m, Math.Round(result.HistoricalAverageIncome!.Value, 2));

        // Historical avg expense: (1400+1450)/2/12 ≈ 118.75
        Assert.NotNull(result.HistoricalAverageExpense);
        Assert.Equal(118.75m, Math.Round(result.HistoricalAverageExpense!.Value, 2));
    }

    [Fact]
    public void Compute_NoTransactions_AllZeros()
    {
        // Arrange
        List<Transaction> transactions = new();

        // Act
        AnnualDashboardMonthlyDto result = MonthlyEvolutionService.Compute(transactions, 2026, null);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Incomes, v => Assert.Equal(0m, v));
        Assert.All(result.Expenses, v => Assert.Equal(0m, v));
        Assert.All(result.Savings, v => Assert.Equal(0m, v));
        Assert.Null(result.BestIncomeMonth);
        Assert.Null(result.BestExpenseMonth);
        Assert.Null(result.HistoricalAverageIncome);
    }
}
