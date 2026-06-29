namespace SauronSheet.Application.Tests.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Services;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Tests for TimelineService (REQ-009).
/// Strict TDD: RED — Tests first, then implement.
/// Task 2.4: Detects chronological events from transactions.
/// </summary>
[Trait("Category", "Application")]
public class TimelineServiceTests
{
    private static readonly UserId TestUserId = new("test-user");

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
    public void Compute_WithMultipleTransactions_ReturnsFourPlusEvents()
    {
        // Arrange
        List<Transaction> transactions = new()
        {
            // First transaction of the year
            CreateTransaction(100m, new DateTime(2026, 1, 2), null, "First income"),
            // Highest income
            CreateTransaction(5000m, new DateTime(2026, 6, 15), SubcategoryId.New(), "Bonus"),
            // Biggest expense
            CreateTransaction(-3000m, new DateTime(2026, 3, 10), null, "Car repair"),
            // Last transaction
            CreateTransaction(200m, new DateTime(2026, 12, 30), null, "Last income"),
            // Some extras
            CreateTransaction(2000m, new DateTime(2026, 4, 1), null, "Salary Apr"),
            CreateTransaction(-500m, new DateTime(2026, 4, 5), null, "Rent Apr"),
        };

        // Act
        IReadOnlyList<TimelineEventDto> events = TimelineService.Compute(transactions, 2026);

        // Assert
        Assert.NotNull(events);
        Assert.True(events.Count >= 4); // At least 4 events

        // Should contain highest-income event
        TimelineEventDto? highestIncome = null;
        foreach (TimelineEventDto e in events)
        {
            if (e.Type == "highest-income")
            {
                highestIncome = e;
                break;
            }
        }
        Assert.NotNull(highestIncome);
        Assert.Equal(5000m, highestIncome!.Amount);
        Assert.Equal("2026-06-15", highestIncome.Date);

        // Should contain biggest-expense event
        TimelineEventDto? biggestExpense = null;
        foreach (TimelineEventDto e in events)
        {
            if (e.Type == "biggest-expense")
            {
                biggestExpense = e;
                break;
            }
        }
        Assert.NotNull(biggestExpense);
        Assert.Equal(3000m, biggestExpense!.Amount);

        // Should have first-transaction
        TimelineEventDto? firstTrx = null;
        foreach (TimelineEventDto e in events)
        {
            if (e.Type == "first-transaction")
            {
                firstTrx = e;
                break;
            }
        }
        Assert.NotNull(firstTrx);
    }

    [Fact]
    public void Compute_FewerThanFourEvents_ReturnsExisting()
    {
        // Arrange: only 2 transactions
        List<Transaction> transactions = new()
        {
            CreateTransaction(1000m, new DateTime(2026, 6, 15), null, "Income"),
            CreateTransaction(-500m, new DateTime(2026, 6, 10), null, "Expense"),
        };

        // Act
        IReadOnlyList<TimelineEventDto> events = TimelineService.Compute(transactions, 2026);

        // Assert — events exist but may be fewer than 4
        Assert.NotNull(events);
        Assert.True(events.Count > 0);
        Assert.True(events.Count <= 4); // 2 trx → at most 4 events (highest, biggest, first, last - some overlap)
    }

    [Fact]
    public void Compute_NoTransactions_ReturnsEmptyList()
    {
        // Arrange
        List<Transaction> transactions = new();

        // Act
        IReadOnlyList<TimelineEventDto> events = TimelineService.Compute(transactions, 2026);

        // Assert
        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public void Compute_OnlyExpenses_ReturnsExpenseEvents()
    {
        // Arrange
        List<Transaction> transactions = new()
        {
            CreateTransaction(-100m, new DateTime(2026, 1, 5), null, "Small expense"),
            CreateTransaction(-2000m, new DateTime(2026, 6, 15), null, "Big expense"),
            CreateTransaction(-500m, new DateTime(2026, 3, 10), null, "Medium expense"),
        };

        // Act
        IReadOnlyList<TimelineEventDto> events = TimelineService.Compute(transactions, 2026);

        // Assert
        Assert.NotNull(events);
        Assert.NotEmpty(events);

        // Should include biggest-expense
        TimelineEventDto? biggestExpense = null;
        foreach (TimelineEventDto e in events)
        {
            if (e.Type == "biggest-expense")
            {
                biggestExpense = e;
                break;
            }
        }
        Assert.NotNull(biggestExpense);
        Assert.Equal(2000m, biggestExpense!.Amount);
    }
}
