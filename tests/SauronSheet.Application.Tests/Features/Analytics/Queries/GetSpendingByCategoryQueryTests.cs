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
/// Tests for GetSpendingByCategoryQueryHandler.
/// Phase 4 (US2): Spending breakdown by category (pie chart).
/// </summary>
public class GetSpendingByCategoryQueryTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly GetSpendingByCategoryQueryHandler _handler;

    public GetSpendingByCategoryQueryTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns("user-1");
        _handler = new GetSpendingByCategoryQueryHandler(
            _transactionRepoMock.Object,
            _categoryRepoMock.Object,
            _userContextMock.Object);
    }

    private static Transaction CreateExpense(decimal amount, CategoryId? categoryId, DateTime? date = null)
    {
        return new Transaction(
            new TransactionId(Guid.NewGuid()),
            new UserId("user-1"),
            new Money(amount, "EUR"),  // negative for expenses
            date ?? new DateTime(2026, 1, 15),
            "Test expense",
            categoryId);
    }

    private static Category CreateCategory(Guid id, string name, string? color = null)
    {
        return TestCategoryFactory.CreateUserCategory(categoryId: new CategoryId(id), userId: new UserId("user-1"), name: name, color: color);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetSpendingByCategory_WithTransactions_ReturnsGroupedData()
    {
        // Arrange
        var cat1Id = Guid.NewGuid();
        var cat2Id = Guid.NewGuid();
        var cat3Id = Guid.NewGuid();

        var transactions = new List<Transaction>
        {
            CreateExpense(-300m, new CategoryId(cat1Id)),
            CreateExpense(-200m, new CategoryId(cat1Id)),
            CreateExpense(-150m, new CategoryId(cat2Id)),
            CreateExpense(-100m, new CategoryId(cat3Id)),
            CreateExpense(-50m, new CategoryId(cat3Id))
        };

        var categories = new List<Category>
        {
            CreateCategory(cat1Id, "Food", "#FF0000"),
            CreateCategory(cat2Id, "Transport", "#00FF00"),
            CreateCategory(cat3Id, "Entertainment", "#0000FF")
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(
            new GetSpendingByCategoryQuery(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)),
            CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(500m, result[0].Amount);  // Food: 300 + 200
        Assert.Equal(150m, result[1].Amount);  // Transport: 150
        Assert.Equal(150m, result[2].Amount);  // Entertainment: 100 + 50
        // Total = 800, Food = 500/800 = 62.5%
        Assert.Equal(62.5m, result[0].Percentage);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetSpendingByCategory_NoTransactions_ReturnsEmptyList()
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
            new GetSpendingByCategoryQuery(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)),
            CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetSpendingByCategory_OnlyIncomeTransactions_ReturnsEmptyList()
    {
        // Arrange — only positive amounts (income)
        var transactions = new List<Transaction>
        {
            CreateExpense(500m, new CategoryId(Guid.NewGuid())),
            CreateExpense(300m, new CategoryId(Guid.NewGuid()))
        };

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(new List<Category>());

        // Act
        var result = await _handler.Handle(
            new GetSpendingByCategoryQuery(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)),
            CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task GetSpendingByCategory_MoreThanSixCategories_GroupsIntoOther()
    {
        // Arrange — 8 categories
        var catIds = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToArray();
        var transactions = catIds.Select(id =>
            CreateExpense(-100m, new CategoryId(id))).ToList();
        var categories = catIds.Select((id, i) =>
            CreateCategory(id, $"Cat{i + 1}")).ToList();

        _transactionRepoMock
            .Setup(x => x.FindBySpecificationAsync(It.IsAny<ISpecification<Transaction>>()))
            .ReturnsAsync(transactions);
        _categoryRepoMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(
            new GetSpendingByCategoryQuery(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)),
            CancellationToken.None);

        // Assert
        Assert.Equal(7, result.Count); // top 6 + "Other"
        Assert.Equal("Other", result.Last().CategoryName);
        Assert.Equal("#6B7280", result.Last().CategoryColor);
    }
}
