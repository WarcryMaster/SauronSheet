namespace SauronSheet.Application.Tests.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Services;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

[Trait("Category", "Application")]
public class AnomalyDetectionServiceTests
{
    private static readonly UserId TestUserId = new("test-user");

    private static Transaction CreateExpense(decimal amount, int year, int month, string category)
    {
        return new Transaction(
            id: new TransactionId(Guid.NewGuid()),
            userId: TestUserId,
            amount: new Money(-Math.Abs(amount), "EUR"),
            date: new DateTime(year, month, 10),
            description: category,
            bankCategory: category);
    }

    [Fact]
    public void Compute_WhenAmountExceedsMeanPlusTwoStd_ReturnsAnomaly()
    {
        Dictionary<int, List<Transaction>> byYear = new()
        {
            [2024] = new() { CreateExpense(100m, 2024, 8, "Food") },
            [2025] = new() { CreateExpense(100m, 2025, 8, "Food") },
            [2026] = new() { CreateExpense(300m, 2026, 8, "Food") },
        };

        IReadOnlyList<AnomalyDto> anomalies = AnomalyDetectionService.Compute(byYear, 2026);

        Assert.Single(anomalies);
        Assert.Equal("Food", anomalies[0].Category);
        Assert.Equal(8, anomalies[0].Month);
    }

    [Fact]
    public void Compute_WhenAmountExceedsThreeTimesMean_ReturnsExtraordinary()
    {
        Dictionary<int, List<Transaction>> byYear = new()
        {
            [2024] = new() { CreateExpense(100m, 2024, 3, "Travel") },
            [2025] = new() { CreateExpense(100m, 2025, 3, "Travel") },
            [2026] = new() { CreateExpense(1000m, 2026, 3, "Travel") },
        };

        IReadOnlyList<AnomalyDto> anomalies = AnomalyDetectionService.Compute(byYear, 2026);

        Assert.Single(anomalies);
        Assert.Equal("extraordinary", anomalies[0].Type);
    }

    [Fact]
    public void Compute_WhenSpikeIsIsolated_ReturnsExceptional()
    {
        Dictionary<int, List<Transaction>> byYear = new()
        {
            [2024] = new() { CreateExpense(80m, 2024, 11, "Utilities") },
            [2025] = new() { CreateExpense(90m, 2025, 10, "Utilities") },
            [2026] = new() { CreateExpense(300m, 2026, 11, "Utilities") },
        };

        IReadOnlyList<AnomalyDto> anomalies = AnomalyDetectionService.Compute(byYear, 2026);

        Assert.Single(anomalies);
        Assert.Equal("exceptional", anomalies[0].Type);
    }

    [Fact]
    public void Compute_WhenSpikeRepeatsSameMonthPreviousYear_DoesNotReturnExceptional()
    {
        Dictionary<int, List<Transaction>> byYear = new()
        {
            [2024] = new() { CreateExpense(80m, 2024, 6, "Health") },
            [2025] = new() { CreateExpense(260m, 2025, 6, "Health") },
            [2026] = new() { CreateExpense(900m, 2026, 6, "Health") },
        };

        IReadOnlyList<AnomalyDto> anomalies = AnomalyDetectionService.Compute(byYear, 2026);

        Assert.Single(anomalies);
        Assert.NotEqual("exceptional", anomalies[0].Type);
    }

    [Fact]
    public void Compute_WhenNoAnomalies_ReturnsEmpty()
    {
        Dictionary<int, List<Transaction>> byYear = new()
        {
            [2024] = new() { CreateExpense(100m, 2024, 1, "Food") },
            [2025] = new() { CreateExpense(105m, 2025, 1, "Food") },
            [2026] = new() { CreateExpense(107m, 2026, 1, "Food") },
        };

        IReadOnlyList<AnomalyDto> anomalies = AnomalyDetectionService.Compute(byYear, 2026);

        Assert.Empty(anomalies);
    }
}
