namespace SauronSheet.Application.Tests.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Services;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Tests for FinancialRatiosService (REQ-011).
/// Strict TDD: RED → Tests first.
/// Task 3.2: Computes financial ratios from transactions.
/// </summary>
[Trait("Category", "Application")]
public class FinancialRatiosServiceTests
{
    private static readonly UserId TestUserId = new("test-user");

    private static Transaction CreateTransaction(decimal amount, DateTime date, string description = "Test")
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            TestUserId,
            new Money(amount, "EUR"),
            date,
            description);
    }

    [Fact]
    public void Compute_EmptyTransactions_AllRatiosNull()
    {
        // Arrange
        List<Transaction> transactions = new();

        // Act
        AnnualDashboardRatiosDto result = FinancialRatiosService.Compute(transactions, 2026);

        // Assert
        Assert.Null(result.SavingsRate);
        Assert.Null(result.AverageMonthlyIncome);
        Assert.Null(result.AverageMonthlyExpense);
        Assert.Null(result.AverageMonthlySavings);
        Assert.Null(result.AverageDailyExpense);
        Assert.Null(result.AveragePerTransaction);
        Assert.Equal(0, result.TransactionCount);
        Assert.Null(result.AverageOperationsPerMonth);
    }

    [Fact]
    public void Compute_SingleTransaction_ReturnsCorrectRatios()
    {
        // Arrange: one income transaction
        List<Transaction> transactions = new()
        {
            CreateTransaction(3000m, new DateTime(2026, 1, 15), "Salary"),
        };

        // Act
        AnnualDashboardRatiosDto result = FinancialRatiosService.Compute(transactions, 2026);

        // Assert: monthly averages = totals / 12
        Assert.Equal(250m, result.AverageMonthlyIncome);      // 3000/12
        Assert.Null(result.AverageMonthlyExpense);             // no expenses
        Assert.Equal(250m, result.AverageMonthlySavings);      // (3000-0)/12
        Assert.Equal(100m, result.SavingsRate);
        Assert.Equal(3000m, result.AveragePerTransaction);
        Assert.Equal(1, result.TransactionCount);
        Assert.Equal(0.08m, Math.Round(result.AverageOperationsPerMonth!.Value, 2));
    }

    [Fact]
    public void Compute_FullYearData_ReturnsCorrectRatios()
    {
        // Arrange: 12 months of income and expenses
        List<Transaction> transactions = new();
        for (int month = 1; month <= 12; month++)
        {
            transactions.Add(CreateTransaction(3000m, new DateTime(2026, month, 15), $"Salary {month}"));
            transactions.Add(CreateTransaction(-1200m, new DateTime(2026, month, 10), $"Rent {month}"));
            transactions.Add(CreateTransaction(-200m, new DateTime(2026, month, 5), $"Food {month}"));
        }
        // 36 transactions total

        // Act
        AnnualDashboardRatiosDto result = FinancialRatiosService.Compute(transactions, 2026);

        // Assert
        // Income: 12 * 3000 = 36000, Avg monthly: 36000/12 = 3000
        Assert.Equal(36000m, result.AverageMonthlyIncome * 12);
        // Expense: 12 * (1200+200) = 16800, Avg monthly: 16800/12 = 1400
        Assert.Equal(16800m, result.AverageMonthlyExpense * 12);
        // Savings: 36000 - 16800 = 19200, Avg monthly: 1600
        Assert.Equal(19200m, result.AverageMonthlySavings * 12);
        // Savings rate: 19200/36000*100 = 53.33%
        Assert.Equal(53.33m, Math.Round(result.SavingsRate!.Value, 2));
        // Avg daily expense: 16800/365
        Assert.Equal(46.03m, Math.Round(result.AverageDailyExpense!.Value, 2));
        // Avg per transaction: (36000+16800)/36
        Assert.Equal(1466.67m, Math.Round(result.AveragePerTransaction!.Value, 2));
        Assert.Equal(36, result.TransactionCount);
        // Avg ops per month: 36/12
        Assert.Equal(3m, result.AverageOperationsPerMonth);
    }

    [Fact]
    public void Compute_OnlyExpenses_SavingsRateIsNull()
    {
        // Arrange: only expense transactions, no income
        List<Transaction> transactions = new()
        {
            CreateTransaction(-500m, new DateTime(2026, 1, 10), "Rent"),
            CreateTransaction(-100m, new DateTime(2026, 1, 15), "Food"),
        };

        // Act
        AnnualDashboardRatiosDto result = FinancialRatiosService.Compute(transactions, 2026);

        // Assert
        Assert.Null(result.SavingsRate); // 0 income → div by zero
        Assert.Null(result.AverageMonthlyIncome);
        Assert.Equal(50m, result.AverageMonthlyExpense);  // 600/12
        Assert.Null(result.AverageMonthlySavings); // net ≤ 0 → null per W3
        Assert.Equal(300m, result.AveragePerTransaction);
        Assert.Equal(2, result.TransactionCount);
    }

    [Fact]
    public void Compute_ZeroTransactions_ReturnsNullForDivisionByZero()
    {
        // Arrange
        List<Transaction> transactions = new()
        {
            CreateTransaction(0m, new DateTime(2026, 1, 1), "Zero"),
        };

        // Act: zero-amount transactions should be filtered
        AnnualDashboardRatiosDto result = FinancialRatiosService.Compute(transactions, 2026);

        // Assert
        Assert.Equal(0, result.TransactionCount);
        Assert.Null(result.SavingsRate);
        Assert.Null(result.AverageMonthlyIncome);
    }
}
