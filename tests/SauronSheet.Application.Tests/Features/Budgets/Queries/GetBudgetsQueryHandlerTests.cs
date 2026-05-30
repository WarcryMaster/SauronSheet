using Moq;
using Xunit;

using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Application.Tests.Features.Budgets.Queries;

public class GetBudgetsQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private GetBudgetsQueryHandler CreateHandler()
    {
        return new GetBudgetsQueryHandler(
            _budgetRepoMock.Object,
            _categoryRepoMock.Object,
            _userContextMock.Object);
    }

    private void SetupUser(string userId = "user-1")
    {
        _userContextMock.Setup(u => u.UserId).Returns(userId);
    }

    private static Budget CreateBudget(
        string userId = "user-1",
        Guid? categoryId = null,
        DateOnly? effectiveFrom = null,
        DateOnly? effectiveUntil = null,
        BudgetPeriod period = BudgetPeriod.Monthly,
        decimal limit = 500m)
    {
        return new Budget(
            new BudgetId(Guid.NewGuid()),
            new UserId(userId),
            new CategoryId(categoryId ?? Guid.NewGuid()),
            effectiveFrom ?? new DateOnly(2026, 1, 1),
            effectiveUntil,
            period,
            new Money(limit, "EUR"));
    }

    private static Category CreateCategory(Guid id, string name)
    {
        return new Category(
            new CategoryId(id),
            new UserId("user-1"),
            new CategoryName(name),
            CategoryType.Expense,
            new ColorHex("#000000"),
            "default");
    }

    // ── Happy path: returns all budgets when AsOf is null ──────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_AsOfNull_ReturnsAllUserBudgets()
    {
        // Arrange
        SetupUser("user-1");
        var catId1 = Guid.NewGuid();
        var catId2 = Guid.NewGuid();
        var budget1 = CreateBudget(categoryId: catId1, limit: 500m);
        var budget2 = CreateBudget(categoryId: catId2, limit: 300m);

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget> { budget1, budget2 });

        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Category>
            {
                CreateCategory(catId1, "Groceries"),
                CreateCategory(catId2, "Transport")
            });

        var query = new GetBudgetsQuery();
        var handler = CreateHandler();

        // Act
        List<BudgetDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(budget1.Id.Value, result[0].Id);
        Assert.Equal(catId1, result[0].CategoryId);
        Assert.Equal("Groceries", result[0].CategoryName);
        Assert.Equal(500m, result[0].Limit);
        Assert.Equal(budget2.Id.Value, result[1].Id);
        Assert.Equal("Transport", result[1].CategoryName);
        Assert.Equal(300m, result[1].Limit);
    }

    // ── Filters by AsOf date ───────────────────────────────────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_AsOfSpecified_FiltersActiveOnDate()
    {
        // Arrange
        SetupUser("user-1");
        var catId = Guid.NewGuid();
        var activeBudget = CreateBudget(
            categoryId: catId,
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: new DateOnly(2026, 6, 30));
        var inactiveBudget = CreateBudget(
            effectiveFrom: new DateOnly(2026, 7, 1));

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget> { activeBudget, inactiveBudget });

        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Category>
            {
                CreateCategory(catId, "Groceries"),
                CreateCategory(inactiveBudget.CategoryId.Value, "Future")
            });

        var query = new GetBudgetsQuery(AsOf: new DateOnly(2026, 3, 15));
        var handler = CreateHandler();

        // Act
        List<BudgetDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(activeBudget.Id.Value, result[0].Id);
    }

    // ── Permanent budget (EffectiveUntil = null) is always active

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_AsOfSpecified_PermanentBudgetAlwaysActive()
    {
        // Arrange
        SetupUser("user-1");
        var catId = Guid.NewGuid();
        var permanent = CreateBudget(
            categoryId: catId,
            effectiveFrom: new DateOnly(2026, 1, 1),
            effectiveUntil: null); // permanent

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget> { permanent });

        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Category>
            {
                CreateCategory(catId, "Groceries")
            });

        var query = new GetBudgetsQuery(AsOf: new DateOnly(2030, 6, 1));
        var handler = CreateHandler();

        // Act
        List<BudgetDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert — permanent budget is active on any future date
        Assert.Single(result);
        Assert.Equal(permanent.Id.Value, result[0].Id);
    }

    // ── Empty list when no budgets ─────────────────────────────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_NoBudgets_ReturnsEmptyList()
    {
        // Arrange
        SetupUser("user-1");

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Budget>());

        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-1")))
            .ReturnsAsync(new List<Category>());

        var query = new GetBudgetsQuery();
        var handler = CreateHandler();

        // Act
        List<BudgetDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    // ── Tenant scoping ─────────────────────────────────────────

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_TenantScoped_UsesCurrentUserContext()
    {
        // Arrange
        SetupUser("user-A");
        var catId = Guid.NewGuid();
        var budget = CreateBudget(userId: "user-A", categoryId: catId, limit: 500m);

        _budgetRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-A")))
            .ReturnsAsync(new List<Budget> { budget });

        _categoryRepoMock
            .Setup(r => r.GetByUserIdAsync(new UserId("user-A")))
            .ReturnsAsync(new List<Category>
            {
                CreateCategory(catId, "Groceries")
            });

        var query = new GetBudgetsQuery();
        var handler = CreateHandler();

        // Act
        List<BudgetDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result);
        _budgetRepoMock.Verify(r => r.GetByUserIdAsync(new UserId("user-A")), Times.Once);
    }
}
