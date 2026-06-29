namespace SauronSheet.Application.Tests.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

using SauronSheet.Application.Features.Analytics.Classification;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Services;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Tests for AnnualSummaryService (REQ-001).
/// Strict TDD: RED → Tests first.
/// Task 3.1: Pure service that processes transactions + year → AnnualDashboardSummaryDto.
/// </summary>
[Trait("Category", "Application")]
public class AnnualSummaryServiceTests
{
    private static readonly UserId TestUserId = new("test-user");
    private static readonly SubcategoryId SalarySubcategoryId = SubcategoryId.New();

    private static Transaction CreateTransaction(decimal amount, DateTime date, SubcategoryId? subcategoryId = null, string description = "Test")
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            TestUserId,
            new Money(amount, "EUR"),
            date,
            description,
            subcategoryId: subcategoryId);
    }

    [Fact]
    public void Compute_EmptyTransactions_ReturnsZeroSummary()
    {
        // Arrange
        List<Transaction> transactions = new();
        List<AnnualAnalysisRowDto> classifiedRows = new();

        // Act
        AnnualDashboardSummaryDto result = AnnualSummaryService.Compute(
            transactions, 2026, classifiedRows, new Dictionary<SubcategoryId, string>(), 0, 0, null, null, null, null, null);

        // Assert
        Assert.Equal(2026, result.Year);
        Assert.Equal(0m, result.Income);
        Assert.Equal(0m, result.Expense);
        Assert.Equal(0m, result.Net);
        Assert.Equal(0m, result.Savings);
        Assert.Equal(0m, result.SavingsRate);
        Assert.False(result.HasPreviousYear);
        Assert.False(result.HasNextYear);
        Assert.Null(result.YearRank);
        Assert.Equal(0, result.TotalYears);
    }

    [Fact]
    public void Compute_SingleYear_ReturnsCorrectSummary()
    {
        // Arrange
        List<Transaction> transactions = new()
        {
            CreateTransaction(3000m, new DateTime(2026, 1, 15), SalarySubcategoryId, "Salary Jan"),
            CreateTransaction(3000m, new DateTime(2026, 2, 15), SalarySubcategoryId, "Salary Feb"),
            CreateTransaction(3000m, new DateTime(2026, 3, 15), SalarySubcategoryId, "Salary Mar"),
            CreateTransaction(-500m, new DateTime(2026, 1, 10), null, "Rent Jan"),
            CreateTransaction(-500m, new DateTime(2026, 2, 10), null, "Rent Feb"),
            CreateTransaction(-500m, new DateTime(2026, 3, 10), null, "Rent Mar"),
        };

        List<AnnualAnalysisRowDto> classifiedRows = new()
        {
            new AnnualAnalysisRowDto("Salary", AnalysisLineType.IncomeFixed, "Ingreso Fijo",
                750m, new decimal[12] { 3000, 3000, 3000, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "EUR"),
            new AnnualAnalysisRowDto("Rent", AnalysisLineType.ExpenseFixed, "Gasto Fijo",
                125m, new decimal[12] { 500, 500, 500, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "EUR"),
        };

        // Act: range 2024-2026 means current year 2026 has both previous and next
        AnnualDashboardSummaryDto result = AnnualSummaryService.Compute(
            transactions, 2026, classifiedRows,
            new Dictionary<SubcategoryId, string>(),
            2024, 2026, // minYear, maxYear
            null, null, null, null, null);

        // Assert
        Assert.Equal(2026, result.Year);
        Assert.Equal(9000m, result.Income);   // 3 × 3000
        Assert.Equal(1500m, result.Expense);  // 3 × 500
        Assert.Equal(7500m, result.Net);      // 9000 - 1500
        Assert.Equal(7500m, result.Savings);   // net = savings (no previous years)
        Assert.Equal(83.33m, Math.Round(result.SavingsRate, 2)); // 7500/9000*100
        Assert.True(result.HasPreviousYear);
        Assert.False(result.HasNextYear);      // 2026 is the max in range 2024-2026
        Assert.Equal(3, result.TotalYears);
        Assert.Null(result.IncomeChangeAbs);   // no previous year data passed
        Assert.Null(result.IncomeChangePct);
    }

    [Fact]
    public void Compute_WithYoY_ComputesCorrectChanges()
    {
        // Arrange
        List<Transaction> currentYearTransactions = new()
        {
            CreateTransaction(4000m, new DateTime(2026, 1, 15), SalarySubcategoryId, "Salary"),
            CreateTransaction(-600m, new DateTime(2026, 1, 10), null, "Rent"),
        };

        List<AnnualAnalysisRowDto> classifiedRows = new()
        {
            new AnnualAnalysisRowDto("Salary", AnalysisLineType.IncomeFixed, "Ingreso Fijo",
                333.33m, new decimal[12] { 4000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "EUR"),
            new AnnualAnalysisRowDto("Rent", AnalysisLineType.ExpenseFixed, "Gasto Fijo",
                50m, new decimal[12] { 600, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "EUR"),
        };

        // Act: previous year had Income=3000, Expense=500
        AnnualDashboardSummaryDto result = AnnualSummaryService.Compute(
            currentYearTransactions, 2026, classifiedRows,
            new Dictionary<SubcategoryId, string>(),
            2025, 2026,
            3000m,  // prevIncome
            500m,   // prevExpense
            2500m,  // prevNet
            2500m,  // prevSavings
            83.33m); // prevSavingsRate

        // Assert YoY changes
        Assert.True(result.HasPreviousYear);
        Assert.Equal(4000m, result.Income);
        Assert.Equal(600m, result.Expense);
        Assert.Equal(3400m, result.Net);
        Assert.Equal(3400m, result.Savings);

        // Income: (4000 - 3000) = 1000 abs, (1000/3000)*100 = 33.33%
        Assert.Equal(1000m, result.IncomeChangeAbs);
        Assert.Equal(33.33m, result.IncomeChangePct);

        // Expense: (600 - 500) = 100 abs, (100/500)*100 = 20%
        Assert.Equal(100m, result.ExpenseChangeAbs);
        Assert.Equal(20m, result.ExpenseChangePct);

        // Net: (3400 - 2500) = 900 abs, (900/2500)*100 = 36%
        Assert.Equal(900m, result.NetChangeAbs);
        Assert.Equal(36m, result.NetChangePct);

        // Savings rate: (3400/4000)*100 = 85%
        Assert.Equal(85m, result.SavingsRate);
    }

    [Fact]
    public void Compute_RankFirst_WhenNoOtherYears()
    {
        // Arrange
        List<Transaction> transactions = new()
        {
            CreateTransaction(1000m, new DateTime(2026, 1, 15), null, "Income"),
        };
        List<AnnualAnalysisRowDto> classifiedRows = new();

        // Act: only 1 available year → rank should be 1
        AnnualDashboardSummaryDto result = AnnualSummaryService.Compute(
            transactions, 2026, classifiedRows,
            new Dictionary<SubcategoryId, string>(),
            2026, 2026, null, null, null, null, null);

        // Assert
        Assert.Equal(1, result.TotalYears);
        Assert.False(result.HasPreviousYear);
        Assert.False(result.HasNextYear);
    }

    [Fact]
    public void Compute_ZeroIncome_SavingsRateIsZero()
    {
        // Arrange: only expenses, no income
        List<Transaction> transactions = new()
        {
            CreateTransaction(-500m, new DateTime(2026, 1, 10), null, "Expense"),
        };
        List<AnnualAnalysisRowDto> classifiedRows = new();

        List<AnnualAnalysisRowDto> expenseOnlyRows = new()
        {
            new AnnualAnalysisRowDto("Rent", AnalysisLineType.ExpenseFixed, "Gasto Fijo",
                41.67m, new decimal[12] { 500, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "EUR"),
        };

        // Act
        AnnualDashboardSummaryDto result = AnnualSummaryService.Compute(
            transactions, 2026, expenseOnlyRows,
            new Dictionary<SubcategoryId, string>(),
            0, 0, null, null, null, null, null);

        // Assert
        Assert.Equal(0m, result.Income);
        Assert.Equal(500m, result.Expense);
        Assert.Equal(-500m, result.Net);
        Assert.Equal(0m, result.SavingsRate);
    }

    [Fact]
    public void Compute_WithAllYoYNulls_WhenNoPreviousYearData()
    {
        // Arrange
        List<Transaction> transactions = new()
        {
            CreateTransaction(2000m, new DateTime(2026, 6, 15), null, "Income"),
        };
        List<AnnualAnalysisRowDto> classifiedRows = new();

        // Act: no previous year data → pass nulls
        AnnualDashboardSummaryDto result = AnnualSummaryService.Compute(
            transactions, 2026, classifiedRows,
            new Dictionary<SubcategoryId, string>(),
            2026, 2026, null, null, null, null, null);

        // Assert
        Assert.False(result.HasPreviousYear);
        Assert.Null(result.IncomeChangeAbs);
        Assert.Null(result.IncomeChangePct);
        Assert.Null(result.ExpenseChangeAbs);
        Assert.Null(result.ExpenseChangePct);
        Assert.Null(result.NetChangeAbs);
        Assert.Null(result.NetChangePct);
    }
}
