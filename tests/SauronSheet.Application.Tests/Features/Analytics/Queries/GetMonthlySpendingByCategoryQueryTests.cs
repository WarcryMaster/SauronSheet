using SauronSheet.Domain.Common;
using Moq;
using Xunit;

using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Application.Tests.Common;

namespace SauronSheet.Application.Tests.Features.Analytics.Queries;

/// <summary>
/// Tests for GetMonthlySpendingByCategoryQueryHandler.
/// Validates: date-range filtering, sort by total descending, Year field, multi-month grouping.
/// </summary>
public class GetMonthlySpendingByCategoryQueryTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly GetMonthlySpendingByCategoryQueryHandler _handler;

    public GetMonthlySpendingByCategoryQueryTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns("user-1");
        _handler = new GetMonthlySpendingByCategoryQueryHandler(
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _userContextMock.Object);
    }

    private static Transaction CreateExpense(decimal amount, CategoryId? categoryId, DateTime date)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(amount, "EUR"),
            date,
            "Test expense",
            categoryId);
    }

    private static Category CreateCategory(Guid id, string name)
    {
        return TestCategoryFactory.CreateUserCategory(
            categoryId: new CategoryId(id),
            userId: new UserId("user-1"),
            name: name);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_RangeWithExpenses_SortedByTotalDescending()
    {
        // Arrange — Compras=800, Ocio=300, Transporte=150
        var comprasId = Guid.NewGuid();
        var ocioId = Guid.NewGuid();
        var transporteId = Guid.NewGuid();

        var transactions = new List<Transaction>
        {
            CreateExpense(-500m, new CategoryId(comprasId), new DateTime(2026, 4, 10)),
            CreateExpense(-300m, new CategoryId(comprasId), new DateTime(2026, 5, 15)),
            CreateExpense(-300m, new CategoryId(ocioId), new DateTime(2026, 4, 20)),
            CreateExpense(-150m, new CategoryId(transporteId), new DateTime(2026, 6, 5))
        };

        var categories = new List<Category>
        {
            CreateCategory(comprasId, "Compras"),
            CreateCategory(ocioId, "Ocio"),
            CreateCategory(transporteId, "Transporte")
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(
            new GetMonthlySpendingByCategoryQuery(
                new DateTime(2026, 4, 1),
                new DateTime(2026, 6, 30)),
            CancellationToken.None);

        // Assert — categories sorted by total amount descending across the range
        // Compras (800) > Ocio (300) > Transporte (150)
        // But result is per-month entries; verify the ordering is by category total
        // First entries should be Compras (highest total), then Ocio, then Transporte
        var categoryOrder = result
            .Select(r => r.CategoryName)
            .Distinct()
            .ToList();

        Assert.Equal("Compras", categoryOrder[0]);
        Assert.Equal("Ocio", categoryOrder[1]);
        Assert.Equal("Transporte", categoryOrder[2]);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_EmptyRange_ReturnsEmptyList()
    {
        // Arrange
        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(new List<Transaction>());
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _handler.Handle(
            new GetMonthlySpendingByCategoryQuery(
                new DateTime(2026, 4, 1),
                new DateTime(2026, 6, 30)),
            CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_MultiMonthRange_GroupsByYearAndMonth()
    {
        // Arrange — expenses in April and June
        var catId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            CreateExpense(-100m, new CategoryId(catId), new DateTime(2026, 4, 10)),
            CreateExpense(-200m, new CategoryId(catId), new DateTime(2026, 6, 15))
        };

        var categories = new List<Category>
        {
            CreateCategory(catId, "Food")
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(
            new GetMonthlySpendingByCategoryQuery(
                new DateTime(2026, 4, 1),
                new DateTime(2026, 6, 30)),
            CancellationToken.None);

        // Assert — two entries (April and June), each with Year=2026
        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal(2026, dto.Year));
        Assert.Equal(4, result[0].Month);
        Assert.Equal(100m, result[0].Amount);
        Assert.Equal(6, result[1].Month);
        Assert.Equal(200m, result[1].Amount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SingleMonthRange_ReturnsOnlyThatMonth()
    {
        // Arrange — "This Month" scenario: only June
        var catId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            CreateExpense(-250m, new CategoryId(catId), new DateTime(2026, 6, 15))
        };

        var categories = new List<Category>
        {
            CreateCategory(catId, "Food")
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(
            new GetMonthlySpendingByCategoryQuery(
                new DateTime(2026, 6, 1),
                new DateTime(2026, 6, 15)),
            CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(2026, result[0].Year);
        Assert.Equal(6, result[0].Month);
        Assert.Equal("June", result[0].MonthName);
        Assert.Equal("Food", result[0].CategoryName);
        Assert.Equal(250m, result[0].Amount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_RangeChange_ReordersCategories()
    {
        // Arrange — In Q2 Compras=800 is first, but in a different range Ocio=1000 wins
        var comprasId = Guid.NewGuid();
        var ocioId = Guid.NewGuid();

        // These transactions would be returned for a range where Ocio > Compras
        var transactions = new List<Transaction>
        {
            CreateExpense(-200m, new CategoryId(comprasId), new DateTime(2026, 4, 10)),
            CreateExpense(-1000m, new CategoryId(ocioId), new DateTime(2026, 4, 20))
        };

        var categories = new List<Category>
        {
            CreateCategory(comprasId, "Compras"),
            CreateCategory(ocioId, "Ocio")
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(
            new GetMonthlySpendingByCategoryQuery(
                new DateTime(2026, 4, 1),
                new DateTime(2026, 4, 30)),
            CancellationToken.None);

        // Assert — Ocio (1000) now comes before Compras (200)
        var categoryOrder = result
            .Select(r => r.CategoryName)
            .Distinct()
            .ToList();

        Assert.Equal("Ocio", categoryOrder[0]);
        Assert.Equal("Compras", categoryOrder[1]);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_TieInTotal_SortedByNameAscending()
    {
        // Arrange — two categories with identical totals
        var alphaId = Guid.NewGuid();
        var betaId = Guid.NewGuid();

        var transactions = new List<Transaction>
        {
            CreateExpense(-500m, new CategoryId(alphaId), new DateTime(2026, 5, 10)),
            CreateExpense(-500m, new CategoryId(betaId), new DateTime(2026, 5, 15))
        };

        var categories = new List<Category>
        {
            CreateCategory(alphaId, "Beta"),   // name starts with B
            CreateCategory(betaId, "Alpha")    // name starts with A
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(
            new GetMonthlySpendingByCategoryQuery(
                new DateTime(2026, 5, 1),
                new DateTime(2026, 5, 31)),
            CancellationToken.None);

        // Assert — tied totals → alphabetical by name ascending
        var categoryOrder = result
            .Select(r => r.CategoryName)
            .Distinct()
            .ToList();

        Assert.Equal("Alpha", categoryOrder[0]);
        Assert.Equal("Beta", categoryOrder[1]);
    }
}
