namespace SauronSheet.Application.Tests.Features.Analytics.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

using SauronSheet.Application.Features.Analytics.Classification;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Application.Tests.Common;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Integration tests for GetAnnualAnalysisQueryHandler.
/// Uses real AnnualClassificationEngine and mocked repositories.
/// </summary>
public class GetAnnualAnalysisQueryHandlerTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ISubcategoryRepository> _subcategoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly GetAnnualAnalysisQueryHandler _handler;

    public GetAnnualAnalysisQueryHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns("user-1");
        _handler = new GetAnnualAnalysisQueryHandler(
            _transactionRepoMock.Object,
            _subcategoryRepoMock.Object,
            _userContextMock.Object,
            new AnnualClassificationEngine());
    }

    private static Transaction CreateTransaction(
        string userIdValue,
        decimal amount,
        DateTime date,
        SubcategoryId? subcategoryId,
        string description)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId(userIdValue),
            new Money(amount, "EUR"),
            date,
            description,
            subcategoryId: subcategoryId);
    }

    private static Subcategory CreateSubcategory(SubcategoryId id, string name)
    {
        return TestSubcategoryFactory.CreateUserSubcategory(
            subcategoryId: id,
            userId: new UserId("user-1"),
            name: name);
    }

    private void SetupTransactions(List<Transaction> source)
    {
        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync((ISpecification<Transaction> spec) => source.Where(spec.Criteria.Compile()).ToList());
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handler_HasDataFalse_WhenNoTransactions()
    {
        // Arrange
        List<Transaction> source = new();
        SetupTransactions(source);
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Subcategory>());

        // Act
        AnnualAnalysisResultDto result = await _handler.Handle(
            new GetAnnualAnalysisQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.False(result.HasData);
        Assert.Empty(result.Rows);
        Assert.Equal(0m, result.Summary.IncomeFixed);
        Assert.Equal(0m, result.Summary.IncomeVariable);
        Assert.Equal(0m, result.Summary.IncomeTotal);
        Assert.Equal(0m, result.Summary.ExpenseFixed);
        Assert.Equal(0m, result.Summary.ExpenseVariable);
        Assert.Equal(0m, result.Summary.ExpenseTotal);
        Assert.Equal(0m, result.Summary.Net);
        Assert.Equal("EUR", result.Currency);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handler_ReturnsCorrectRowsAndSummary()
    {
        // Arrange
        SubcategoryId salarySubcategoryId = SubcategoryId.New();
        SubcategoryId supermarketSubcategoryId = SubcategoryId.New();

        List<Subcategory> subcategories = new()
        {
            CreateSubcategory(salarySubcategoryId, "Nómina"),
            CreateSubcategory(supermarketSubcategoryId, "Supermercados y alimentación")
        };

        List<Transaction> source = new()
        {
            CreateTransaction("user-1", 1500m, new DateTime(2026, 1, 28), salarySubcategoryId, "Nómina enero"),
            CreateTransaction("user-1", -120m, new DateTime(2026, 2, 10), supermarketSubcategoryId, "Carrefour")
        };

        SetupTransactions(source);
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(subcategories);

        // Act
        AnnualAnalysisResultDto result = await _handler.Handle(
            new GetAnnualAnalysisQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.Equal(2, result.Rows.Count);

        AnnualAnalysisRowDto salaryRow = result.Rows.Single(r => r.Movement == "Nómina");
        AnnualAnalysisRowDto supermarketRow = result.Rows.Single(r => r.Movement == "Supermercados y alimentación");

        Assert.Equal(AnalysisLineType.IncomeFixed, salaryRow.LineType);
        Assert.Equal(AnalysisLineType.ExpenseVariable, supermarketRow.LineType);
        Assert.Equal(1500m / 12, salaryRow.Average);
        Assert.Equal(120m / 12, supermarketRow.Average);
        Assert.Equal(1500m, salaryRow.MonthlyAmounts[0]);
        Assert.Equal(120m, supermarketRow.MonthlyAmounts[1]);

        Assert.Equal(1500m, result.Summary.IncomeFixed);
        Assert.Equal(0m, result.Summary.IncomeVariable);
        Assert.Equal(1500m, result.Summary.IncomeTotal);
        Assert.Equal(0m, result.Summary.ExpenseFixed);
        Assert.Equal(120m, result.Summary.ExpenseVariable);
        Assert.Equal(120m, result.Summary.ExpenseTotal);
        Assert.Equal(1380m, result.Summary.Net);
        Assert.Equal("EUR", result.Currency);
        Assert.Equal(2, result.Summary.MonthsWithData);
    }

    // ---- MonthsWithData scenarios (PR 1 - Phase 2) ----

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handler_HasNoMonthsWithData_WhenNoTransactions()
    {
        // Arrange
        List<Transaction> source = new();
        SetupTransactions(source);
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Subcategory>());

        // Act
        AnnualAnalysisResultDto result = await _handler.Handle(
            new GetAnnualAnalysisQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.False(result.HasData);
        Assert.Equal(0, result.Summary.MonthsWithData);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handler_MonthsWithData_5Months()
    {
        // Arrange — 5 distinct months (Jan through May)
        SubcategoryId foodId = SubcategoryId.New();
        List<Subcategory> subcategories = new()
        {
            CreateSubcategory(foodId, "Alimentación")
        };

        List<Transaction> source = new();
        for (int month = 1; month <= 5; month++)
        {
            source.Add(CreateTransaction(
                "user-1", -100m,
                new DateTime(2026, month, 15),
                foodId, $"Compra mes {month}"));
        }

        SetupTransactions(source);
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(subcategories);

        // Act
        AnnualAnalysisResultDto result = await _handler.Handle(
            new GetAnnualAnalysisQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.Equal(5, result.Summary.MonthsWithData);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handler_MonthsWithData_12Months()
    {
        // Arrange — all 12 months
        SubcategoryId foodId = SubcategoryId.New();
        List<Subcategory> subcategories = new()
        {
            CreateSubcategory(foodId, "Alimentación")
        };

        List<Transaction> source = new();
        for (int month = 1; month <= 12; month++)
        {
            source.Add(CreateTransaction(
                "user-1", -100m,
                new DateTime(2026, month, 15),
                foodId, $"Compra mes {month}"));
        }

        SetupTransactions(source);
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(subcategories);

        // Act
        AnnualAnalysisResultDto result = await _handler.Handle(
            new GetAnnualAnalysisQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.Equal(12, result.Summary.MonthsWithData);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handler_TenantIsolation_OnlyUserTransactions()
    {
        // Arrange
        SubcategoryId electricitySubcategoryId = SubcategoryId.New();
        List<Subcategory> subcategories = new()
        {
            CreateSubcategory(electricitySubcategoryId, "Luz y gas")
        };

        List<Transaction> source = new()
        {
            CreateTransaction("user-1", -50m, new DateTime(2026, 3, 5), electricitySubcategoryId, "Iberdrola"),
            CreateTransaction("user-2", -75m, new DateTime(2026, 4, 5), null, "Otro usuario")
        };

        SetupTransactions(source);
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(subcategories);

        // Act
        AnnualAnalysisResultDto result = await _handler.Handle(
            new GetAnnualAnalysisQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.Single(result.Rows);
        Assert.Equal("Luz y gas", result.Rows[0].Movement);
        Assert.Equal(50m, result.Rows[0].MonthlyAmounts.Sum());
        Assert.Equal(50m, result.Summary.ExpenseFixed);
        Assert.Equal(0m, result.Summary.ExpenseVariable);
        Assert.Equal(50m, result.Summary.ExpenseTotal);
        Assert.Equal(0m, result.Summary.IncomeTotal);
        Assert.Equal(-50m, result.Summary.Net);
        Assert.Equal(1, result.Summary.MonthsWithData);
    }

    // ==================== PR 2 — YoY Variation (Phase 3) ====================

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handler_YoYVariation_HappyPath_ComputesCorrectPercentages()
    {
        // Arrange — two consecutive years with clear variation:
        //   2025: IncomeFixed=1000, ExpenseFixed=200  → Net=800
        //   2026: IncomeFixed=1200, ExpenseFixed=170  → Net=1030
        // Expected:
        //   IncomeFixedPct  = (1200-1000)/|1000|*100 =  20m
        //   ExpenseFixedPct = (170-200)/|200|*100     = -15m
        //   IncomeTotalPct  = 20m  (same as IncomeFixed, no variable income)
        //   ExpenseTotalPct = -15m (same as ExpenseFixed, no variable expense)
        //   NetPct          = (1030-800)/|800|*100     = 28.75m
        //   IncomeVariablePct    = null (previous=0)
        //   ExpenseVariablePct   = null (previous=0)
        //   HasPreviousYearData  = true
        SubcategoryId payrollId = SubcategoryId.New();
        SubcategoryId utilitiesId = SubcategoryId.New();

        List<Subcategory> subcategories = new()
        {
            CreateSubcategory(payrollId, "Nómina"),
            CreateSubcategory(utilitiesId, "Luz y gas")
        };

        List<Transaction> source = new()
        {
            // 2025 — previous year
            CreateTransaction("user-1", 1000m, new DateTime(2025, 1, 28), payrollId, "Nómina 2025"),
            CreateTransaction("user-1", -200m, new DateTime(2025, 2, 10), utilitiesId, "Luz 2025"),
            // 2026 — current year
            CreateTransaction("user-1", 1200m, new DateTime(2026, 1, 28), payrollId, "Nómina 2026"),
            CreateTransaction("user-1", -170m, new DateTime(2026, 2, 10), utilitiesId, "Luz 2026")
        };

        SetupTransactions(source);
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(subcategories);

        // Act
        AnnualAnalysisResultDto result = await _handler.Handle(
            new GetAnnualAnalysisQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.NotNull(result.Summary.Variation);
        Assert.True(result.Summary.Variation.HasPreviousYearData);

        Assert.Equal(20m, result.Summary.Variation.IncomeFixedPct);
        Assert.Equal(20m, result.Summary.Variation.IncomeTotalPct);
        Assert.Equal(-15m, result.Summary.Variation.ExpenseFixedPct);
        Assert.Equal(-15m, result.Summary.Variation.ExpenseTotalPct);
        Assert.Equal(28.75m, result.Summary.Variation.NetPct);

        // Zero-division from previous year (previous IncomeVariable=0 → null)
        Assert.Null(result.Summary.Variation.IncomeVariablePct);
        Assert.Null(result.Summary.Variation.ExpenseVariablePct);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handler_YoYVariation_ZeroDivisionGuard_ReturnsNullForZeroPrevious()
    {
        // Arrange — previous year has IncomeFixed=0 but non-zero ExpenseFixed.
        // Variation for IncomeFixed should be null (division by zero guard).
        //   2025: IncomeFixed=0,       ExpenseFixed=200
        //   2026: IncomeFixed=500,     ExpenseFixed=300
        // Expected:
        //   IncomeFixedPct   = null (previous=0)
        //   IncomeTotalPct   = null (previous=0)
        //   ExpenseFixedPct  = (300-200)/|200|*100 = 50m
        //   ExpenseTotalPct  = 50m
        //   NetPct           = (200 - (-200))/|-200|*100 = 200m
        //   HasPreviousYearData = true
        SubcategoryId utilitiesId = SubcategoryId.New();

        List<Subcategory> subcategories = new()
        {
            CreateSubcategory(utilitiesId, "Luz y gas")
        };

        List<Transaction> source = new()
        {
            // 2025 — only expense, no income
            CreateTransaction("user-1", -200m, new DateTime(2025, 6, 15), utilitiesId, "Luz 2025"),
            // 2026 — income + expense
            CreateTransaction("user-1", 500m, new DateTime(2026, 1, 28), null, "Ingreso 2026"),
            CreateTransaction("user-1", -300m, new DateTime(2026, 6, 15), utilitiesId, "Luz 2026")
        };

        SetupTransactions(source);
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(subcategories);

        // Act
        AnnualAnalysisResultDto result = await _handler.Handle(
            new GetAnnualAnalysisQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.NotNull(result.Summary.Variation);
        Assert.True(result.Summary.Variation.HasPreviousYearData);

        // Income fields should be null because previous year IncomeFixed = 0
        Assert.Null(result.Summary.Variation.IncomeFixedPct);
        Assert.Null(result.Summary.Variation.IncomeVariablePct);
        Assert.Null(result.Summary.Variation.IncomeTotalPct);

        // Expense fields should have computed values
        Assert.Equal(50m, result.Summary.Variation.ExpenseFixedPct);
        Assert.Equal(50m, result.Summary.Variation.ExpenseTotalPct);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handler_YoYVariation_EmptyPreviousYear_VariationIsNull()
    {
        // Arrange — only 2026 has transactions, 2025 has none.
        // Previous year load returns hasData=false → Variation stays null.
        SubcategoryId payrollId = SubcategoryId.New();
        List<Subcategory> subcategories = new()
        {
            CreateSubcategory(payrollId, "Nómina")
        };

        List<Transaction> source = new()
        {
            CreateTransaction("user-1", 1500m, new DateTime(2026, 1, 28), payrollId, "Nómina 2026")
        };

        SetupTransactions(source);
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(subcategories);

        // Act
        AnnualAnalysisResultDto result = await _handler.Handle(
            new GetAnnualAnalysisQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.Null(result.Summary.Variation);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handler_YoYVariation_PartialPreviousYear_StillComputes()
    {
        // Arrange — 2025 has 3 months of data, 2026 has full year.
        // Variation should still compute based on annual totals.
        //   2025 (3mo): IncomeFixed=600 (3×200),   ExpenseFixed=300 (3×100)
        //   2026 (12mo): IncomeFixed=1200 (12×100), ExpenseFixed=600 (12×50)
        // Expected:
        //   IncomeFixedPct  = (1200-600)/|600|*100  = 100m
        //   ExpenseFixedPct = (600-300)/|300|*100   = 100m
        //   IncomeTotalPct  = 100m
        //   ExpenseTotalPct = 100m
        //   NetPct          = (600-300)/|300|*100   = 100m
        //   HasPreviousYearData = true
        SubcategoryId payrollId = SubcategoryId.New();
        SubcategoryId utilitiesId = SubcategoryId.New();

        List<Subcategory> subcategories = new()
        {
            CreateSubcategory(payrollId, "Nómina"),
            CreateSubcategory(utilitiesId, "Luz y gas")
        };

        List<Transaction> source = new();

        // 2025 — 3 months
        for (int month = 1; month <= 3; month++)
        {
            source.Add(CreateTransaction(
                "user-1", 200m,
                new DateTime(2025, month, 15),
                payrollId, $"Nómina 2025-{month:00}"));
            source.Add(CreateTransaction(
                "user-1", -100m,
                new DateTime(2025, month, 20),
                utilitiesId, $"Luz 2025-{month:00}"));
        }

        // 2026 — 12 months
        for (int month = 1; month <= 12; month++)
        {
            source.Add(CreateTransaction(
                "user-1", 100m,
                new DateTime(2026, month, 15),
                payrollId, $"Nómina 2026-{month:00}"));
            source.Add(CreateTransaction(
                "user-1", -50m,
                new DateTime(2026, month, 20),
                utilitiesId, $"Luz 2026-{month:00}"));
        }

        SetupTransactions(source);
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(subcategories);

        // Act
        AnnualAnalysisResultDto result = await _handler.Handle(
            new GetAnnualAnalysisQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.NotNull(result.Summary.Variation);
        Assert.True(result.Summary.Variation.HasPreviousYearData);

        Assert.Equal(100m, result.Summary.Variation.IncomeFixedPct);
        Assert.Equal(100m, result.Summary.Variation.ExpenseFixedPct);
        Assert.Equal(100m, result.Summary.Variation.IncomeTotalPct);
        Assert.Equal(100m, result.Summary.Variation.ExpenseTotalPct);
        Assert.Equal(100m, result.Summary.Variation.NetPct);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handler_YoYVariation_NegativePreviousNet_UsesAbsoluteValue()
    {
        // Arrange — previous year Net is negative (expenses > income).
        // Formula uses |previous| so a negative Net should invert correctly.
        //   2025: IncomeFixed=1000, ExpenseFixed=1200 → Net=-200
        //   2026: IncomeFixed=1200, ExpenseFixed=1000 → Net=200
        // Expected:
        //   NetPct = (200 - (-200)) / |-200| * 100 = 200m
        //   HasPreviousYearData = true
        SubcategoryId payrollId = SubcategoryId.New();
        SubcategoryId utilitiesId = SubcategoryId.New();

        List<Subcategory> subcategories = new()
        {
            CreateSubcategory(payrollId, "Nómina"),
            CreateSubcategory(utilitiesId, "Luz y gas")
        };

        List<Transaction> source = new()
        {
            // 2025 — more expenses than income → negative Net
            CreateTransaction("user-1", 1000m, new DateTime(2025, 1, 28), payrollId, "Nómina 2025"),
            CreateTransaction("user-1", -1200m, new DateTime(2025, 6, 10), utilitiesId, "Luz 2025"),
            // 2026 — more income than expenses → positive Net
            CreateTransaction("user-1", 1200m, new DateTime(2026, 1, 28), payrollId, "Nómina 2026"),
            CreateTransaction("user-1", -1000m, new DateTime(2026, 6, 10), utilitiesId, "Luz 2026")
        };

        SetupTransactions(source);
        _subcategoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(subcategories);

        // Act
        AnnualAnalysisResultDto result = await _handler.Handle(
            new GetAnnualAnalysisQuery(2026),
            CancellationToken.None);

        // Assert
        Assert.True(result.HasData);
        Assert.NotNull(result.Summary.Variation);
        Assert.True(result.Summary.Variation.HasPreviousYearData);

        Assert.Equal(200m, result.Summary.Variation.NetPct);
    }
}