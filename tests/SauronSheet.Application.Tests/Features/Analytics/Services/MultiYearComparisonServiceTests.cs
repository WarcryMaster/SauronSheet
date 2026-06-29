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
/// Tests for MultiYearComparisonService (REQ-003).
/// Strict TDD: RED — Tests first, then implement.
/// Task 2.1: Pure service that aggregates income/expense/savings/balance by year.
/// </summary>
[Trait("Category", "Application")]
public class MultiYearComparisonServiceTests
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
    public void Compute_MultipleYears_ReturnsCorrectAggregates()
    {
        // Arrange
        SubcategoryId salaryId = SubcategoryId.New();

        // Build 5 years of data
        Dictionary<int, List<Transaction>> transactionsByYear = new()
        {
            { 2022, new List<Transaction>
                {
                    CreateTransaction(30000m, new DateTime(2022, 6, 15), salaryId),
                    CreateTransaction(-18000m, new DateTime(2022, 6, 10), null),
                }
            },
            { 2023, new List<Transaction>
                {
                    CreateTransaction(35000m, new DateTime(2023, 6, 15), salaryId),
                    CreateTransaction(-20000m, new DateTime(2023, 6, 10), null),
                }
            },
            { 2024, new List<Transaction>
                {
                    CreateTransaction(40000m, new DateTime(2024, 6, 15), salaryId),
                    CreateTransaction(-25000m, new DateTime(2024, 6, 10), null),
                }
            },
            { 2025, new List<Transaction>
                {
                    CreateTransaction(45000m, new DateTime(2025, 6, 15), salaryId),
                    CreateTransaction(-28000m, new DateTime(2025, 6, 10), null),
                }
            },
            { 2026, new List<Transaction>
                {
                    CreateTransaction(50000m, new DateTime(2026, 6, 15), salaryId),
                    CreateTransaction(-30000m, new DateTime(2026, 6, 10), null),
                }
            },
        };

        // Act
        AnnualDashboardMultiYearDto? result = MultiYearComparisonService.Compute(transactionsByYear, 2026);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result!.Years.Count);

        // Incomes: 30000, 35000, 40000, 45000, 50000
        Assert.Equal(50000m, result.Incomes[4]);
        Assert.Equal(30000m, result.Incomes[0]);

        // Expenses: 18000, 20000, 25000, 28000, 30000
        Assert.Equal(30000m, result.Expenses[4]);
        Assert.Equal(18000m, result.Expenses[0]);

        // Savings: 12000, 15000, 15000, 17000, 20000
        Assert.Equal(20000m, result.Savings[4]);
        Assert.Equal(12000m, result.Savings[0]);

        // Balances match savings
        Assert.Equal(result.Savings[0], result.Balances[0]);

        // Highlight year
        Assert.Equal(2026, result.HighlightYear);

        // Best year: 2026 (highest savings 20000)
        Assert.Equal(2026, result.BestYear);

        // Worst year: 2022 (lowest savings 12000)
        Assert.Equal(2022, result.WorstYear);

        // Previous year (2025)
        Assert.NotNull(result.PreviousYearValue);
        Assert.Equal(45000m, result.PreviousYearValue!.Income);
        Assert.Equal(28000m, result.PreviousYearValue!.Expense);

        // No next year (2026 is the max)
        Assert.Null(result.NextYearValue);

        // Average
        Assert.NotNull(result.Average);
        Assert.Equal(40000m, result.Average!.Income);  // (30000+35000+40000+45000+50000)/5
        Assert.Equal(24200m, result.Average!.Expense); // (18000+20000+25000+28000+30000)/5
    }

    [Fact]
    public void Compute_SingleYear_ReturnsNull()
    {
        // Arrange
        Dictionary<int, List<Transaction>> transactionsByYear = new()
        {
            { 2026, new List<Transaction>
                {
                    CreateTransaction(50000m, new DateTime(2026, 6, 15)),
                    CreateTransaction(-30000m, new DateTime(2026, 6, 10)),
                }
            },
        };

        // Act
        AnnualDashboardMultiYearDto? result = MultiYearComparisonService.Compute(transactionsByYear, 2026);

        // Assert — single year should return null (hidden from UI)
        Assert.Null(result);
    }

    [Fact]
    public void Compute_ThreeYears_BestWorstAvgCorrect()
    {
        // Arrange
        Dictionary<int, List<Transaction>> transactionsByYear = new()
        {
            { 2024, new List<Transaction>
                {
                    CreateTransaction(40000m, new DateTime(2024, 6, 15)),
                    CreateTransaction(-25000m, new DateTime(2024, 6, 10)),
                }
            },
            { 2025, new List<Transaction>
                {
                    CreateTransaction(35000m, new DateTime(2025, 6, 15)),
                    CreateTransaction(-20000m, new DateTime(2025, 6, 10)),
                }
            },
            { 2026, new List<Transaction>
                {
                    CreateTransaction(50000m, new DateTime(2026, 6, 15)),
                    CreateTransaction(-30000m, new DateTime(2026, 6, 10)),
                }
            },
        };

        // Act
        AnnualDashboardMultiYearDto? result = MultiYearComparisonService.Compute(transactionsByYear, 2025);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Years.Count);

        // Best: 2026 (20000 savings)
        Assert.Equal(2026, result.BestYear);

        // Worst: 2024 (15000 savings)
        Assert.Equal(2024, result.WorstYear);

        // Average income: (40000+35000+50000)/3 = 41666.67
        Assert.Equal(41666.67m, Math.Round(result.Average!.Income, 2));

        // Previous year exists (2024)
        Assert.NotNull(result.PreviousYearValue);

        // Next year exists (2026)
        Assert.NotNull(result.NextYearValue);
    }

    [Fact]
    public void Compute_WithOnlyExpenses_HandlesNegativeSavings()
    {
        // Arrange
        Dictionary<int, List<Transaction>> transactionsByYear = new()
        {
            { 2025, new List<Transaction>
                {
                    CreateTransaction(10000m, new DateTime(2025, 6, 15)),
                    CreateTransaction(-15000m, new DateTime(2025, 6, 10)),
                }
            },
            { 2026, new List<Transaction>
                {
                    CreateTransaction(12000m, new DateTime(2026, 6, 15)),
                    CreateTransaction(-18000m, new DateTime(2026, 6, 10)),
                }
            },
        };

        // Act
        AnnualDashboardMultiYearDto? result = MultiYearComparisonService.Compute(transactionsByYear, 2026);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Years.Count);

        // Both years have negative savings
        Assert.True(result.Savings[0] < 0m);
        Assert.True(result.Savings[1] < 0m);

        // Best year: 2025 (-5000 > -6000)
        Assert.Equal(2025, result.BestYear);

        // Worst year: 2026 (-6000)
        Assert.Equal(2026, result.WorstYear);
    }
}
