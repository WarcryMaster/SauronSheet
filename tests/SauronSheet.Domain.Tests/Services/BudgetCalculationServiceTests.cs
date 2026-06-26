using System;
using Xunit;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Domain.Tests.Services;

public class BudgetCalculationServiceTests
{
    private readonly BudgetCalculationService _service = new();

    // ═══════════════════════════════════════════════════════════════
    // Task 2.2–2.3: PeriodsElapsed — Monthly
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Monthly_SingleFullMonth_ReturnsOne()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Monthly,
            from: new DateOnly(2026, 5, 1),
            to: new DateOnly(2026, 5, 31));

        Assert.Equal(1, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Monthly_PartialMonth_CountsAsComplete()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Monthly,
            from: new DateOnly(2026, 5, 1),
            to: new DateOnly(2026, 5, 15));

        Assert.Equal(1, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Monthly_TwelveFullMonths_ReturnsTwelve()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Monthly,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 12, 31));

        Assert.Equal(12, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Monthly_CrossingThreeMonths_ReturnsThree()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Monthly,
            from: new DateOnly(2026, 1, 15),
            to: new DateOnly(2026, 3, 10));

        Assert.Equal(3, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Monthly_CrossingYearBoundary_ReturnsCorrectCount()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Monthly,
            from: new DateOnly(2025, 11, 1),
            to: new DateOnly(2026, 2, 28));

        Assert.Equal(4, result); // Nov, Dec, Jan, Feb
    }

    // ═══════════════════════════════════════════════════════════════
    // Task 2.3: PeriodsElapsed — Quarterly
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Quarterly_FullQuarter_ReturnsOne()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Quarterly,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 3, 31));

        Assert.Equal(1, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Quarterly_TwoQuarters_ReturnsTwo()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Quarterly,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 6, 30));

        Assert.Equal(2, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Quarterly_PartialCrossesTwoQuarters_ReturnsTwo()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Quarterly,
            from: new DateOnly(2026, 2, 15),
            to: new DateOnly(2026, 4, 10));

        Assert.Equal(2, result); // Q1 (Feb) + Q2 (Apr)
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Quarterly_SingleDay_ReturnsOne()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Quarterly,
            from: new DateOnly(2026, 5, 15),
            to: new DateOnly(2026, 5, 15));

        Assert.Equal(1, result);
    }

    // ═══════════════════════════════════════════════════════════════
    // Task 2.3: PeriodsElapsed — Semester
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Semester_FullH1_ReturnsOne()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Semester,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 6, 30));

        Assert.Equal(1, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Semester_BothSemesters_ReturnsTwo()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Semester,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 12, 31));

        Assert.Equal(2, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Semester_CrossingH1toH2_ReturnsTwo()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Semester,
            from: new DateOnly(2026, 6, 1),
            to: new DateOnly(2026, 7, 31));

        Assert.Equal(2, result); // touches H1 and H2
    }

    // ═══════════════════════════════════════════════════════════════
    // Task 2.3: PeriodsElapsed — Annual
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Annual_FullYear_ReturnsOne()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Annual,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 12, 31));

        Assert.Equal(1, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Annual_TwoFullYears_ReturnsTwo()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Annual,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2027, 12, 31));

        Assert.Equal(2, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_Annual_PartialYear_ReturnsOne()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Annual,
            from: new DateOnly(2026, 6, 1),
            to: new DateOnly(2026, 12, 31));

        Assert.Equal(1, result);
    }

    // ═══════════════════════════════════════════════════════════════
    // Task 2.4: Calculate — intersection with validity period
    // ═══════════════════════════════════════════════════════════════

    private static Budget CreateMonthlyBudget(
        DateOnly effectiveFrom,
        DateOnly? effectiveUntil = null,
        decimal limitAmount = 500m,
        string? budgetId = null)
    {
        return new Budget(
            new BudgetId(Guid.Parse(budgetId ?? Guid.NewGuid().ToString())),
            new UserId("user-test"),
            new CategoryId(Guid.NewGuid()),
            effectiveFrom,
            effectiveUntil,
            BudgetPeriod.Monthly,
            new Money(limitAmount, "EUR"));
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_MonthlyBudget_StartedMidYear_OnlyCountsMonthsAfterEffectiveFrom()
    {
        var budget = CreateMonthlyBudget(
            effectiveFrom: new DateOnly(2026, 4, 1));
        var spent = new Money(0m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 12, 31),
            spent);

        Assert.Equal(9, result.PeriodsElapsed); // April through December
        Assert.Equal(4500m, result.AccumulatedLimit.Amount);
        Assert.Equal("EUR", result.AccumulatedLimit.Currency);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_BudgetWithEffectiveUntil_QueryAfterEnd_ReturnsZeroPeriods()
    {
        var budget = CreateMonthlyBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 6, 30));
        var spent = new Money(0m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 7, 1),
            to: new DateOnly(2026, 12, 31),
            spent);

        Assert.Equal(0, result.PeriodsElapsed);
        Assert.Equal(0m, result.AccumulatedLimit.Amount);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_BudgetEndsMidRange_OnlyCountsPeriodsUntilEffectiveUntil()
    {
        var budget = CreateMonthlyBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 6, 30));
        var spent = new Money(0m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 12, 31),
            spent);

        Assert.Equal(6, result.PeriodsElapsed); // Jan to June
        Assert.Equal(3000m, result.AccumulatedLimit.Amount);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_BudgetStartsAndEndsWithinRange_CorrectPeriods()
    {
        var budget = CreateMonthlyBudget(
            effectiveFrom: new DateOnly(2026, 3, 1),
            effectiveUntil: new DateOnly(2026, 8, 31));
        var spent = new Money(0m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 12, 31),
            spent);

        Assert.Equal(6, result.PeriodsElapsed); // March to August
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_PermanentBudget_LargeRange_CountsAllPeriods()
    {
        var budget = CreateMonthlyBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: null); // permanent
        var spent = new Money(0m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 12, 31),
            spent);

        Assert.Equal(12, result.PeriodsElapsed);
        Assert.Equal(6000m, result.AccumulatedLimit.Amount);
    }

    // ═══════════════════════════════════════════════════════════════
    // Task 2.5: Derived metrics — AccumulatedLimit, Remaining, PercentageUsed, StatusLevel
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_OneMonth_UnderBudget_GreenStatus()
    {
        var budget = CreateMonthlyBudget(
            effectiveFrom: new DateOnly(2026, 5, 1),
            limitAmount: 500m);
        var spent = new Money(300m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 5, 1),
            to: new DateOnly(2026, 5, 31),
            spent);

        Assert.Equal(1, result.PeriodsElapsed);
        Assert.Equal(500m, result.AccumulatedLimit.Amount);
        Assert.Equal(300m, result.Spent.Amount);
        Assert.Equal(200m, result.Remaining.Amount); // 500 - 300
        Assert.Equal(60m, result.PercentageUsed);     // 300/500 * 100
        Assert.Equal(BudgetStatusLevel.Green, result.StatusLevel);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_OneMonth_OverBudget_OverageStatus()
    {
        var budget = CreateMonthlyBudget(
            effectiveFrom: new DateOnly(2026, 5, 1),
            limitAmount: 500m);
        var spent = new Money(600m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 5, 1),
            to: new DateOnly(2026, 5, 31),
            spent);

        Assert.Equal(1, result.PeriodsElapsed);
        Assert.Equal(500m, result.AccumulatedLimit.Amount);
        Assert.Equal(600m, result.Spent.Amount);
        Assert.Equal(-100m, result.Remaining.Amount);
        Assert.Equal(120m, result.PercentageUsed);
        Assert.Equal(BudgetStatusLevel.Overage, result.StatusLevel);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_At75Percent_YellowStatus()
    {
        var budget = CreateMonthlyBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            limitAmount: 100m);
        var spent = new Money(75m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 1, 31),
            spent);

        Assert.Equal(75m, result.PercentageUsed);
        Assert.Equal(BudgetStatusLevel.Yellow, result.StatusLevel);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_At100Percent_RedStatus()
    {
        var budget = CreateMonthlyBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            limitAmount: 100m);
        var spent = new Money(100m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 1, 31),
            spent);

        Assert.Equal(100m, result.PercentageUsed);
        Assert.Equal(BudgetStatusLevel.Red, result.StatusLevel);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_MultiplePeriods_WithSpending_CorrectMetrics()
    {
        var budget = CreateMonthlyBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            limitAmount: 500m);
        var spent = new Money(400m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 3, 31),
            spent);

        Assert.Equal(3, result.PeriodsElapsed);
        Assert.Equal(1500m, result.AccumulatedLimit.Amount); // 500 × 3
        Assert.Equal(400m, result.Spent.Amount);
        Assert.Equal(1100m, result.Remaining.Amount); // 1500 - 400
        Assert.True(result.PercentageUsed > 0 && result.PercentageUsed < 75);
        Assert.Equal(BudgetStatusLevel.Green, result.StatusLevel);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_SpentEqualsZero_GreenStatus()
    {
        var budget = CreateMonthlyBudget(
            effectiveFrom: new DateOnly(2026, 1, 1),
            limitAmount: 500m);
        var spent = new Money(0m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 1, 31),
            spent);

        Assert.Equal(0m, result.PercentageUsed);
        Assert.Equal(BudgetStatusLevel.Green, result.StatusLevel);
        Assert.Equal(500m, result.Remaining.Amount);
    }

    // ═══════════════════════════════════════════════════════════════
    // Edge cases
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_SameDay_ReturnsOne()
    {
        int result = _service.PeriodsElapsed(
            BudgetPeriod.Monthly,
            from: new DateOnly(2026, 5, 15),
            to: new DateOnly(2026, 5, 15));

        Assert.Equal(1, result);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_QuarterlyBudget_WithValidityIntersection()
    {
        var budget = new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId("user-test"),
            new CategoryId(Guid.NewGuid()),
            effectiveFrom: new DateOnly(2026, 4, 1),
            effectiveUntil: new DateOnly(2026, 9, 30),
            BudgetPeriod.Quarterly,
            new Money(1500m, "EUR"));
        var spent = new Money(2000m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 12, 31),
            spent);

        // Q2 (April-June) + Q3 (July-September) = 2 quarters
        Assert.Equal(2, result.PeriodsElapsed);
        Assert.Equal(3000m, result.AccumulatedLimit.Amount); // 1500 × 2
        Assert.Equal(2000m, result.Spent.Amount);
        Assert.Equal(1000m, result.Remaining.Amount);
    }

    [Fact]
    [Trait("Category", "Domain")]
    public void Calculate_AnnualBudget_WithValidityIntersection()
    {
        var budget = new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId("user-test"),
            new CategoryId(Guid.NewGuid()),
            effectiveFrom: new DateOnly(2025, 1, 1),
            effectiveUntil: null,
            BudgetPeriod.Annual,
            new Money(12000m, "EUR"));
        var spent = new Money(8000m, "EUR");

        BudgetCalculationResult result = _service.Calculate(
            budget,
            from: new DateOnly(2026, 1, 1),
            to: new DateOnly(2026, 12, 31),
            spent);

        Assert.Equal(1, result.PeriodsElapsed);
        Assert.Equal(12000m, result.AccumulatedLimit.Amount);
        Assert.Equal(8000m, result.Spent.Amount);
        Assert.Equal(4000m, result.Remaining.Amount);
    }

    // ═══════════════════════════════════════════════════════════════
    // Switch default — ArgumentOutOfRangeException
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Domain")]
    public void PeriodsElapsed_InvalidPeriod_ThrowsArgumentOutOfRangeException()
    {
        var invalidPeriod = (BudgetPeriod)int.MaxValue;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.PeriodsElapsed(invalidPeriod, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)));
    }
}
