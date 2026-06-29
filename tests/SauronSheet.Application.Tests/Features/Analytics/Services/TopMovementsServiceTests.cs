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
/// Tests for TopMovementsService (REQ-010).
/// Strict TDD: RED — Tests first, then implement.
/// Task 2.5: Top 10 expense, top 10 income, most frequent movements.
/// </summary>
[Trait("Category", "Application")]
public class TopMovementsServiceTests
{
    private static readonly UserId TestUserId = new("test-user");
    private static readonly SubcategoryId TestSubId = SubcategoryId.New();

    private static Transaction CreateTransaction(
        decimal amount,
        DateTime date,
        SubcategoryId? subcategoryId = null,
        string description = "Test",
        string? bankCategory = null)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            TestUserId,
            new Money(amount, "EUR"),
            date,
            description,
            subcategoryId: subcategoryId,
            bankCategory: bankCategory);
    }

    [Fact]
    public void Compute_Top10ExpenseIncome_ReturnsBothLists()
    {
        // Arrange: 15 expense transactions and 15 income transactions
        List<Transaction> transactions = new();

        // 15 expenses from -10 to -150
        for (int i = 1; i <= 15; i++)
        {
            transactions.Add(CreateTransaction(
                -i * 10m,
                new DateTime(2026, 1, i),
                TestSubId,
                $"Expense {i}",
                "General"));
        }

        // 15 incomes from 10 to 150
        for (int i = 1; i <= 15; i++)
        {
            transactions.Add(CreateTransaction(
                i * 10m,
                new DateTime(2026, 2, i),
                TestSubId,
                $"Income {i}",
                "Salary"));
        }

        // Act
        TopMovementsResult result = TopMovementsService.Compute(transactions, 2026);

        // Assert
        Assert.NotNull(result);

        // Top 10 expenses (largest absolute values)
        Assert.Equal(10, result.TopExpenses.Count);
        Assert.Equal(150m, result.TopExpenses[0].Amount);  // -150 → 150 abs
        Assert.Equal("expense", result.TopExpenses[0].Type);
        Assert.Equal(60m, result.TopExpenses[9].Amount);    // -60 → 60 abs

        // Top 10 incomes
        Assert.Equal(10, result.TopIncomes.Count);
        Assert.Equal(150m, result.TopIncomes[0].Amount);
        Assert.Equal("income", result.TopIncomes[0].Type);
        Assert.Equal(60m, result.TopIncomes[9].Amount);
    }

    [Fact]
    public void Compute_FewerThanTen_ReturnsAllAvailable()
    {
        // Arrange: only 3 expense, 2 income
        List<Transaction> transactions = new()
        {
            CreateTransaction(-100m, new DateTime(2026, 1, 15), TestSubId, "Big expense"),
            CreateTransaction(-50m, new DateTime(2026, 1, 10), TestSubId, "Medium expense"),
            CreateTransaction(-25m, new DateTime(2026, 1, 5), TestSubId, "Small expense"),
            CreateTransaction(200m, new DateTime(2026, 1, 20), TestSubId, "Income A"),
            CreateTransaction(100m, new DateTime(2026, 1, 25), TestSubId, "Income B"),
        };

        // Act
        TopMovementsResult result = TopMovementsService.Compute(transactions, 2026);

        // Assert
        Assert.Equal(3, result.TopExpenses.Count);
        Assert.Equal(2, result.TopIncomes.Count);
        Assert.NotNull(result.MostFrequent);
    }

    [Fact]
    public void Compute_NoTransactions_ReturnsEmptyWithNoMovements()
    {
        // Arrange
        List<Transaction> transactions = new();

        // Act
        TopMovementsResult result = TopMovementsService.Compute(transactions, 2026);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.TopExpenses);
        Assert.Empty(result.TopIncomes);
        Assert.Empty(result.MostFrequent);
    }

    [Fact]
    public void Compute_WithFrequentDescriptions_ReturnsMostFrequent()
    {
        // Arrange: 5 identical descriptions
        List<Transaction> transactions = new();
        for (int i = 0; i < 5; i++)
        {
            transactions.Add(CreateTransaction(
                -4.50m,
                new DateTime(2026, 1, i + 1),
                TestSubId,
                "Coffee shop",
                "Cafeterias"));
        }

        // Different descriptions
        transactions.Add(CreateTransaction(-100m, new DateTime(2026, 2, 1), null, "Rent"));
        transactions.Add(CreateTransaction(-50m, new DateTime(2026, 2, 15), null, "Transport"));

        // Act
        TopMovementsResult result = TopMovementsService.Compute(transactions, 2026);

        // Assert
        Assert.NotEmpty(result.MostFrequent);
        TopMovementDto firstFrequent = result.MostFrequent[0];
        Assert.Equal("Coffee shop", firstFrequent.Description);
        Assert.Equal(4.50m, firstFrequent.Amount);
        Assert.Equal("frequent", firstFrequent.Type);
        Assert.Null(firstFrequent.TransactionId); // No single transaction for aggregated
    }
}
