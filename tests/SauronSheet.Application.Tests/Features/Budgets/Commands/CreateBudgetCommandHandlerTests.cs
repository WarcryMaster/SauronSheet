using SauronSheet.Domain.Common;
using Xunit;
using Moq;

using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;
using SauronSheet.Application.Tests.Common;

namespace SauronSheet.Application.Tests.Features.Budgets.Commands;

public class CreateBudgetCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUserContext> _userContextMock = new();

    private CreateBudgetCommandHandler CreateHandler()
    {
        var budgetService = new BudgetService(_budgetRepoMock.Object);
        return new CreateBudgetCommandHandler(
            _budgetRepoMock.Object,
            _categoryRepoMock.Object,
            budgetService,
            _userContextMock.Object);
    }

    private void SetupUser(string userId = "user-1")
    {
        _userContextMock.Setup(u => u.UserId).Returns(userId);
    }

    private Category CreateCategory(CategoryId categoryId, UserId userId)
    {
        return TestCategoryFactory.CreateUserCategory(categoryId: categoryId, userId: userId, name: "Groceries");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ValidBudget_CreatesBudgetAndReturnsId()
    {
        // Arrange
        SetupUser();
        var categoryId = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var category = CreateCategory(categoryId, userId);

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(
                It.IsAny<UserId>(), It.IsAny<CategoryId>(), It.IsAny<DateRange>()))
            .ReturnsAsync((Budget?)null);

        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        _budgetRepoMock.Verify(r => r.AddAsync(It.Is<Budget>(b =>
            b.Limit.Amount == 500m &&
            b.CategoryId == categoryId &&
            b.UserId.Value == "user-1")), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DuplicateBudget_ThrowsDomainException()
    {
        // Arrange
        SetupUser();
        var categoryId = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var category = CreateCategory(categoryId, userId);
        var period = new DateRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        var existingBudget = new Budget(
            new BudgetId(Guid.NewGuid()), userId, categoryId, period, new Money(500));

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(userId, categoryId, period))
            .ReturnsAsync(existingBudget);

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_CategoryNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        SetupUser();
        var categoryId = new CategoryId(Guid.NewGuid());

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync((Category?)null);

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ZeroLimit_ThrowsDomainException()
    {
        // Arrange
        SetupUser();
        var categoryId = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var category = CreateCategory(categoryId, userId);

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(
                It.IsAny<UserId>(), It.IsAny<CategoryId>(), It.IsAny<DateRange>()))
            .ReturnsAsync((Budget?)null);

        var command = new CreateBudgetCommand(
            categoryId.Value, 0m,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_TenantScoped_UsesCurrentUserContext()
    {
        // Arrange
        SetupUser("user-A");
        var categoryId = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-A");
        var category = CreateCategory(categoryId, userId);

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(
                It.IsAny<UserId>(), It.IsAny<CategoryId>(), It.IsAny<DateRange>()))
            .ReturnsAsync((Budget?)null);

        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>()))
            .Returns(Task.CompletedTask);

        var command = new CreateBudgetCommand(
            categoryId.Value, 500m,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _budgetRepoMock.Verify(r => r.AddAsync(It.Is<Budget>(b =>
            b.UserId.Value == "user-A")), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DriftedPeriodStart_NormalizesToMonthOfReceivedDateTime()
    {
        // Contract: the handler extracts Year/Month directly from the raw PeriodStart
        // DateTime value WITHOUT timezone conversion.  It strips any time component and
        // non-first-day offset, producing canonical first/last-day boundaries for whatever
        // month the incoming DateTime reports.
        //
        // Known limitation: if a browser in UTC+2 submits April 30 22:00 UTC intending
        // "May 2026", the raw Month is 4, so the handler persists April 2026 boundaries —
        // NOT May 2026.  Timezone correction is out of scope for this handler; the UI layer
        // is responsible for sending a date whose Month already matches the user's intent.
        SetupUser();
        var categoryId = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var category = CreateCategory(categoryId, userId);

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(
                It.IsAny<UserId>(), It.IsAny<CategoryId>(), It.IsAny<DateRange>()))
            .ReturnsAsync((Budget?)null);

        Budget? captured = null;
        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>()))
            .Callback<Budget>(b => captured = b)
            .Returns(Task.CompletedTask);

        // UTC+2 browser intending May 2026 sends April 30 22:00 UTC.
        // The handler sees Month = 4 and normalises to April 2026 canonical boundaries.
        var driftedStart = new DateTime(2026, 4, 30, 22, 0, 0, DateTimeKind.Utc);
        var command = new CreateBudgetCommand(
            categoryId.Value, 300m,
            driftedStart, new DateTime(2026, 5, 31));

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert: persisted budget uses the raw Month value (4 = April), not the
        // intended local month (5 = May).  Time component is stripped; boundaries are
        // first and last day of April 2026.
        Assert.NotNull(captured);
        Assert.Equal(2026, captured!.Period.StartDate.Year);
        Assert.Equal(4,    captured.Period.StartDate.Month); // April — Month of the received DateTime
        Assert.Equal(1,    captured.Period.StartDate.Day);   // first day of that month
        Assert.Equal(30,   captured.Period.EndDate.Day);     // last day of April
        Assert.Equal(4,    captured.Period.EndDate.Month);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_PeriodStartMidMonthOrWithTime_NormalizesToFirstAndLastDay()
    {
        // Regression: any time component or non-first-day date in PeriodStart must be
        // stripped. The handler must derive canonical boundaries purely from Year/Month.
        SetupUser();
        var categoryId = new CategoryId(Guid.NewGuid());
        var userId = new UserId("user-1");
        var category = CreateCategory(categoryId, userId);

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        _budgetRepoMock
            .Setup(r => r.GetByUserAndCategoryAndMonthAsync(
                It.IsAny<UserId>(), It.IsAny<CategoryId>(), It.IsAny<DateRange>()))
            .ReturnsAsync((Budget?)null);

        Budget? captured = null;
        _budgetRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Budget>()))
            .Callback<Budget>(b => captured = b)
            .Returns(Task.CompletedTask);

        // Mid-month start with a time component — should still produce May 2026 boundaries.
        var command = new CreateBudgetCommand(
            categoryId.Value, 200m,
            new DateTime(2026, 5, 15, 10, 30, 0),   // mid-month, mid-day
            new DateTime(2026, 5, 31));

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(2026, captured!.Period.StartDate.Year);
        Assert.Equal(5,    captured.Period.StartDate.Month);
        Assert.Equal(1,    captured.Period.StartDate.Day);
        Assert.Equal(0,    captured.Period.StartDate.Hour);
        Assert.Equal(5,    captured.Period.EndDate.Month);
        Assert.Equal(31,   captured.Period.EndDate.Day); // May has 31 days
    }
}
