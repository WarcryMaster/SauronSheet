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
/// Tests for CategoryAnalysisService (REQ-005, REQ-006, REQ-007).
/// Strict TDD: RED — Tests first, then implement.
/// Task 2.3: Groups classified rows by category, computes ranking and YoY.
/// </summary>
[Trait("Category", "Application")]
public class CategoryAnalysisServiceTests
{
    private static SubcategoryId SubA = SubcategoryId.New();
    private static SubcategoryId SubB = SubcategoryId.New();
    private static SubcategoryId SubC = SubcategoryId.New();

    private static AnnualAnalysisRowDto CreateRow(string movement, bool isIncome, decimal annualAmount, SubcategoryId? subId)
    {
        decimal[] monthlyAmounts = new decimal[12];
        monthlyAmounts[0] = annualAmount; // Put all in January for simplicity
        return new AnnualAnalysisRowDto(
            movement,
            isIncome ? AnalysisLineType.IncomeFixed : AnalysisLineType.ExpenseFixed,
            isIncome ? "Income Fixed" : "Expense Fixed",
            annualAmount / 12m,
            monthlyAmounts,
            "EUR");
    }

    [Fact]
    public void ComputeCategories_MultipleCategories_ReturnsRanked()
    {
        // Arrange
        IReadOnlyList<AnnualAnalysisRowDto> classifiedRows = new List<AnnualAnalysisRowDto>
        {
            CreateRow("Supermarket", false, 12000m, SubA),
            CreateRow("Rent", false, 8000m, SubB),
            CreateRow("Transport", false, 5000m, SubC),
        }.AsReadOnly();

        // Act — categories only (no YoY data)
        (IReadOnlyList<CategoryItemDto> categories, CategoryComparisonTableDto? table) =
            CategoryAnalysisService.ComputeCategories(classifiedRows, null, null);

        // Assert
        Assert.NotNull(categories);
        Assert.Equal(3, categories.Count);

        // Should be sorted by amount descending
        Assert.Equal("Supermarket", categories[0].CategoryName);
        Assert.Equal(12000m, categories[0].Amount);
        Assert.Equal(1, categories[0].Rank);

        Assert.Equal("Rent", categories[1].CategoryName);
        Assert.Equal(8000m, categories[1].Amount);
        Assert.Equal(2, categories[1].Rank);

        Assert.Equal("Transport", categories[2].CategoryName);
        Assert.Equal(5000m, categories[2].Amount);
        Assert.Equal(3, categories[2].Rank);

        // Percentage: 12000/25000 = 48%, 8000/25000 = 32%, 5000/25000 = 20%
        Assert.Equal(48m, categories[0].Percentage);
        Assert.Equal(32m, categories[1].Percentage);
        Assert.Equal(20m, categories[2].Percentage);

        // All are not new (no YoY data passed)
        Assert.False(categories[0].IsNewThisYear);
    }

    [Fact]
    public void ComputeCategories_WithYoY_ComputesChanges()
    {
        // Arrange — current year
        IReadOnlyList<AnnualAnalysisRowDto> currentRows = new List<AnnualAnalysisRowDto>
        {
            CreateRow("Supermarket", false, 12000m, SubA),
            CreateRow("Rent", false, 8000m, SubB),
        }.AsReadOnly();

        // Previous year rows
        IReadOnlyList<AnnualAnalysisRowDto> prevRows = new List<AnnualAnalysisRowDto>
        {
            CreateRow("Supermarket", false, 10000m, SubA),
            CreateRow("Rent", false, 7500m, SubB),
        }.AsReadOnly();

        // Act
        (IReadOnlyList<CategoryItemDto> categories, CategoryComparisonTableDto? table) =
            CategoryAnalysisService.ComputeCategories(currentRows, prevRows, null);

        // Assert
        Assert.Equal(2, categories.Count);

        // Supermarket: YoY 12000-10000 = 2000, (2000/10000)*100 = 20%
        Assert.Equal(2000m, categories[0].YoYChangeAbs);
        Assert.Equal(20m, categories[0].YoYChangePct);
        Assert.Equal("up", categories[0].Trend);
        Assert.False(categories[0].IsNewThisYear);

        // Rent: YoY 8000-7500 = 500, (500/7500)*100 = 6.67%
        Assert.Equal(500m, categories[1].YoYChangeAbs);
        Assert.Equal(6.67m, Math.Round(categories[1].YoYChangePct!.Value, 2));
        Assert.Equal("up", categories[1].Trend);

        // Table should have data since we have prev year
        Assert.NotNull(table);
        Assert.Equal(2, table!.Rows.Count);
        Assert.Equal("Supermarket", table.Rows[0].CategoryName);
    }

    [Fact]
    public void ComputeCategories_NewCategoryThisYear_MarksIsNew()
    {
        // Arrange
        IReadOnlyList<AnnualAnalysisRowDto> currentRows = new List<AnnualAnalysisRowDto>
        {
            CreateRow("Supermarket", false, 12000m, SubA),
            CreateRow("New Subscription", false, 2400m, SubB),
        }.AsReadOnly();

        // Previous year has only Supermarket
        IReadOnlyList<AnnualAnalysisRowDto> prevRows = new List<AnnualAnalysisRowDto>
        {
            CreateRow("Supermarket", false, 10000m, SubA),
        }.AsReadOnly();

        // Act
        (IReadOnlyList<CategoryItemDto> categories, CategoryComparisonTableDto? table) =
            CategoryAnalysisService.ComputeCategories(currentRows, prevRows, null);

        // Assert
        Assert.Equal(2, categories.Count);

        // Supermarket: not new
        Assert.False(categories[0].IsNewThisYear);
        Assert.NotNull(categories[0].YoYChangeAbs);

        // New Subscription: new this year
        Assert.True(categories[1].IsNewThisYear);
        Assert.Null(categories[1].YoYChangeAbs);
        Assert.Equal("new", categories[1].Trend);
    }

    [Fact]
    public void ComputeCategories_NoClassifiableRows_ReturnsEmpty()
    {
        // Arrange
        IReadOnlyList<AnnualAnalysisRowDto> currentRows = Array.Empty<AnnualAnalysisRowDto>();

        // Act
        (IReadOnlyList<CategoryItemDto> categories, CategoryComparisonTableDto? table) =
            CategoryAnalysisService.ComputeCategories(currentRows, null, null);

        // Assert
        Assert.Empty(categories);
        Assert.Null(table);
    }

    [Fact]
    public void ComputeCategories_IncomeRows_FiltersToExpenseOnly()
    {
        // Arrange
        IReadOnlyList<AnnualAnalysisRowDto> currentRows = new List<AnnualAnalysisRowDto>
        {
            CreateRow("Salary", true, 50000m, SubA),
            CreateRow("Supermarket", false, 12000m, SubB),
            CreateRow("Freelance", true, 5000m, SubC),
        }.AsReadOnly();

        // Act
        (IReadOnlyList<CategoryItemDto> categories, CategoryComparisonTableDto? table) =
            CategoryAnalysisService.ComputeCategories(currentRows, null, null);

        // Assert — only expense rows should appear in categories
        Assert.Single(categories);
        Assert.Equal("Supermarket", categories[0].CategoryName);
    }

    [Fact]
    public void ComputeCategories_WithNextYear_ComparisonTableIncludesNext()
    {
        // Arrange
        IReadOnlyList<AnnualAnalysisRowDto> currentRows = new List<AnnualAnalysisRowDto>
        {
            CreateRow("Supermarket", false, 12000m, SubA),
        }.AsReadOnly();

        IReadOnlyList<AnnualAnalysisRowDto> prevRows = new List<AnnualAnalysisRowDto>
        {
            CreateRow("Supermarket", false, 10000m, SubA),
        }.AsReadOnly();

        IReadOnlyList<AnnualAnalysisRowDto> nextRows = new List<AnnualAnalysisRowDto>
        {
            CreateRow("Supermarket", false, 13000m, SubA),
        }.AsReadOnly();

        // Act
        (IReadOnlyList<CategoryItemDto> categories, CategoryComparisonTableDto? table) =
            CategoryAnalysisService.ComputeCategories(currentRows, prevRows, nextRows);

        // Assert comparison table
        Assert.NotNull(table);
        Assert.Single(table!.Rows);
        CategoryComparisonRowDto row = table.Rows[0];

        Assert.Equal("Supermarket", row.CategoryName);
        Assert.Equal(10000m, row.PreviousYearAmount);
        Assert.Equal(12000m, row.SelectedYearAmount);
        Assert.Equal(13000m, row.NextYearAmount);
        Assert.Equal(2000m, row.DiffAbs);
        Assert.Equal(20m, row.DiffPct);
    }

    [Fact]
    public void ComputeCategories_YoYDecrease_SetsDownTrend()
    {
        // Arrange
        IReadOnlyList<AnnualAnalysisRowDto> currentRows = new List<AnnualAnalysisRowDto>
        {
            CreateRow("Supermarket", false, 8000m, SubA),
        }.AsReadOnly();

        IReadOnlyList<AnnualAnalysisRowDto> prevRows = new List<AnnualAnalysisRowDto>
        {
            CreateRow("Supermarket", false, 10000m, SubA),
        }.AsReadOnly();

        // Act
        (IReadOnlyList<CategoryItemDto> categories, _) =
            CategoryAnalysisService.ComputeCategories(currentRows, prevRows, null);

        // Assert
        Assert.Single(categories);
        Assert.Equal(-2000m, categories[0].YoYChangeAbs);
        Assert.Equal(-20m, categories[0].YoYChangePct);
        Assert.Equal("down", categories[0].Trend);
    }
}
