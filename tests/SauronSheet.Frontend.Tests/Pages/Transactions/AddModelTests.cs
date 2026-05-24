using MediatR;
using Moq;
using SauronSheet.Frontend.Pages.Transactions;
using SauronSheet.Application.Features.Categories.Commands;
using SauronSheet.Application.Features.Categories.DTOs;
using SauronSheet.Domain.ValueObjects;
using Xunit;

namespace SauronSheet.Frontend.Tests.Pages.Transactions;

public class AddModelTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly List<CategoryDto> _existingCategories;

    public AddModelTests()
    {
        _mockMediator = new Mock<IMediator>();

        _existingCategories =
        [
            new CategoryDto(
                Id: Guid.NewGuid(),
                Name: "Food",
                Type: "Expense",
                Color: "#E74C3C",
                IconName: "utensils",
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow,
                TransactionCount: 5),
            new CategoryDto(
                Id: Guid.NewGuid(),
                Name: "Transport",
                Type: "Expense",
                Color: "#3498DB",
                IconName: "car",
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow,
                TransactionCount: 3),
            new CategoryDto(
                Id: Guid.NewGuid(),
                Name: "Salary",
                Type: "Income",
                Color: "#2ECC71",
                IconName: "briefcase",
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow,
                TransactionCount: 1),
        ];
    }

    private AddModel CreateModel()
    {
        var model = new AddModel(_mockMediator.Object);
        model.Categories = _existingCategories;
        return model;
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task ResolveCategoryId_NullName_ReturnsNull()
    {
        // Arrange
        var model = CreateModel();
        model.Input.Amount = -50m;

        // Act
        var result = await model.ResolveCategoryIdAsync(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task ResolveCategoryId_EmptyName_ReturnsNull()
    {
        // Arrange
        var model = CreateModel();
        model.Input.Amount = -50m;

        // Act
        var result = await model.ResolveCategoryIdAsync(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task ResolveCategoryId_WhitespaceName_ReturnsNull()
    {
        // Arrange
        var model = CreateModel();
        model.Input.Amount = -50m;

        // Act
        var result = await model.ResolveCategoryIdAsync("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task ResolveCategoryId_ExactMatch_ReturnsMatchingId()
    {
        // Arrange
        var model = CreateModel();
        model.Input.Amount = -50m;

        var expectedId = _existingCategories[0].Id;

        // Act
        var result = await model.ResolveCategoryIdAsync("Food");

        // Assert
        Assert.Equal(expectedId, result);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task ResolveCategoryId_CaseInsensitiveMatch_ReturnsMatchingId()
    {
        // Arrange
        var model = CreateModel();
        model.Input.Amount = -50m;

        var expectedId = _existingCategories[0].Id;

        // Act
        var result = await model.ResolveCategoryIdAsync("food"); // lowercase

        // Assert
        Assert.Equal(expectedId, result);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task ResolveCategoryId_NoMatchExpenseAmount_CreatesExpenseCategory()
    {
        // Arrange
        var model = CreateModel();
        model.Input.Amount = -50m; // negative = expense

        var newCategoryId = Guid.NewGuid();

        _mockMediator
            .Setup(m => m.Send(
                It.Is<CreateCategoryCommand>(c => c.Name == "NewExpense" && c.Type == CategoryType.Expense),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newCategoryId);

        // Act
        var result = await model.ResolveCategoryIdAsync("NewExpense");

        // Assert
        Assert.Equal(newCategoryId, result);
        _mockMediator.Verify(m => m.Send(
            It.Is<CreateCategoryCommand>(c => c.Name == "NewExpense" && c.Type == CategoryType.Expense),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task ResolveCategoryId_NoMatchIncomeAmount_CreatesIncomeCategory()
    {
        // Arrange
        var model = CreateModel();
        model.Input.Amount = 5000m; // positive = income

        var newCategoryId = Guid.NewGuid();

        _mockMediator
            .Setup(m => m.Send(
                It.Is<CreateCategoryCommand>(c => c.Name == "NewIncome" && c.Type == CategoryType.Income),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newCategoryId);

        // Act
        var result = await model.ResolveCategoryIdAsync("NewIncome");

        // Assert
        Assert.Equal(newCategoryId, result);
        _mockMediator.Verify(m => m.Send(
            It.Is<CreateCategoryCommand>(c => c.Name == "NewIncome" && c.Type == CategoryType.Income),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Frontend")]
    public async Task ResolveCategoryId_ZeroAmount_CreatesIncomeCategory()
    {
        // Arrange
        var model = CreateModel();
        model.Input.Amount = 0m; // zero is >= 0, so it should be Income

        var newCategoryId = Guid.NewGuid();

        _mockMediator
            .Setup(m => m.Send(
                It.Is<CreateCategoryCommand>(c => c.Name == "ZeroCat" && c.Type == CategoryType.Income),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newCategoryId);

        // Act
        var result = await model.ResolveCategoryIdAsync("ZeroCat");

        // Assert
        Assert.Equal(newCategoryId, result);
        _mockMediator.Verify(m => m.Send(
            It.Is<CreateCategoryCommand>(c => c.Name == "ZeroCat" && c.Type == CategoryType.Income),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
