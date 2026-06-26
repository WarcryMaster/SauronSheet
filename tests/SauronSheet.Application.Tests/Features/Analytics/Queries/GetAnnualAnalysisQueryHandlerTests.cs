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
}