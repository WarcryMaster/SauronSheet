namespace SauronSheet.Application.Tests.Features.Analytics.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Services;
using SauronSheet.Application.Resources;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;
using Xunit;

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

    private static AnomalyDetectionService CreateService()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        ServiceProvider provider = services.BuildServiceProvider();
        IStringLocalizer<SharedResources> localizer = provider.GetRequiredService<IStringLocalizer<SharedResources>>();
        return new AnomalyDetectionService(localizer);
    }

    private static IDisposable SetCurrentUiCulture(string cultureName)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo target = new CultureInfo(cultureName);
        CultureInfo.CurrentCulture = target;
        CultureInfo.CurrentUICulture = target;
        return new CultureRestorer(originalCulture, originalUiCulture);
    }

    [Fact]
    public void Compute_WhenAmountExceedsMeanPlusTwoStd_ReturnsAnomalyWithSpanishDescription()
    {
        AnomalyDetectionService service = CreateService();
        Dictionary<int, List<Transaction>> byYear = new()
        {
            [2024] = new() { CreateExpense(100m, 2024, 8, "Food") },
            [2025] = new() { CreateExpense(100m, 2025, 8, "Food") },
            [2026] = new() { CreateExpense(300m, 2026, 8, "Food") }
        };

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        IReadOnlyList<AnomalyDto> anomalies = service.Compute(byYear, 2026);

        Assert.Single(anomalies);
        Assert.Equal("Food", anomalies[0].Category);
        Assert.Equal(8, anomalies[0].Month);
        Assert.Contains("anomalía", anomalies[0].Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compute_WhenAmountExceedsThreeTimesMean_ReturnsExtraordinaryWithEnglishDescription()
    {
        AnomalyDetectionService service = CreateService();
        Dictionary<int, List<Transaction>> byYear = new()
        {
            [2024] = new() { CreateExpense(100m, 2024, 3, "Travel") },
            [2025] = new() { CreateExpense(100m, 2025, 3, "Travel") },
            [2026] = new() { CreateExpense(1000m, 2026, 3, "Travel") }
        };

        using IDisposable _ = SetCurrentUiCulture("en-US");
        IReadOnlyList<AnomalyDto> anomalies = service.Compute(byYear, 2026);

        Assert.Single(anomalies);
        Assert.Equal("extraordinary", anomalies[0].Type);
        Assert.Contains("extraordinary expense", anomalies[0].Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compute_WhenSpikeIsIsolated_ReturnsExceptionalWithSpanishDescription()
    {
        AnomalyDetectionService service = CreateService();
        Dictionary<int, List<Transaction>> byYear = new()
        {
            [2024] = new() { CreateExpense(80m, 2024, 11, "Utilities") },
            [2025] = new() { CreateExpense(90m, 2025, 10, "Utilities") },
            [2026] = new() { CreateExpense(300m, 2026, 11, "Utilities") }
        };

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        IReadOnlyList<AnomalyDto> anomalies = service.Compute(byYear, 2026);

        Assert.Single(anomalies);
        Assert.Equal("exceptional", anomalies[0].Type);
        Assert.Contains("pico excepcional", anomalies[0].Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compute_WhenSpikeRepeatsSameMonthPreviousYear_DoesNotReturnExceptional()
    {
        AnomalyDetectionService service = CreateService();
        Dictionary<int, List<Transaction>> byYear = new()
        {
            [2024] = new() { CreateExpense(80m, 2024, 6, "Health") },
            [2025] = new() { CreateExpense(260m, 2025, 6, "Health") },
            [2026] = new() { CreateExpense(900m, 2026, 6, "Health") }
        };

        using IDisposable _ = SetCurrentUiCulture("en-US");
        IReadOnlyList<AnomalyDto> anomalies = service.Compute(byYear, 2026);

        Assert.Single(anomalies);
        Assert.NotEqual("exceptional", anomalies[0].Type);
    }

    [Fact]
    public void Compute_WhenNoAnomalies_ReturnsEmpty()
    {
        AnomalyDetectionService service = CreateService();
        Dictionary<int, List<Transaction>> byYear = new()
        {
            [2024] = new() { CreateExpense(100m, 2024, 1, "Food") },
            [2025] = new() { CreateExpense(105m, 2025, 1, "Food") },
            [2026] = new() { CreateExpense(107m, 2026, 1, "Food") }
        };

        using IDisposable _ = SetCurrentUiCulture("es-ES");
        IReadOnlyList<AnomalyDto> anomalies = service.Compute(byYear, 2026);

        Assert.Empty(anomalies);
    }

    private sealed class CultureRestorer : IDisposable
    {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUiCulture;

        public CultureRestorer(CultureInfo originalCulture, CultureInfo originalUiCulture)
        {
            _originalCulture = originalCulture;
            _originalUiCulture = originalUiCulture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }
}
