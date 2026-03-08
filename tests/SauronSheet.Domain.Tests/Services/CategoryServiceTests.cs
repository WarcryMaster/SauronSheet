namespace SauronSheet.Domain.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.ValueObjects;

[Trait("Category", "Domain")]
public class CategoryServiceTests
{
    private readonly UserId _testUserId = new("user-123");
    private readonly Mock<ICategoryRepository> _mockRepository;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _mockRepository = new Mock<ICategoryRepository>();
        _service = new CategoryService(_mockRepository.Object);
    }

    [Fact]
    public async Task ValidateUniqueName_WithNonexistentName_DoesNotThrow()
    {
        // Arrange
        var name = "New Category";
        _mockRepository
            .Setup(r => r.FindByNameAndUserAsync(_testUserId, name))
            .ReturnsAsync((Category?)null);

        // Act & Assert - Should not throw
        await _service.ValidateUniqueName(_testUserId, name);
    }

    [Fact]
    public async Task ValidateUniqueName_WithDuplicateName_ThrowsDomainException()
    {
        // Arrange
        var name = "Groceries";
        var existingCategory = new Category(
            CategoryId.New(),
            _testUserId,
            CategoryName.Create(name),
            CategoryType.Expense,
            ColorHex.Create("#F39C12"),
            "basket");

        _mockRepository
            .Setup(r => r.FindByNameAndUserAsync(_testUserId, name))
            .ReturnsAsync(existingCategory);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.ValidateUniqueName(_testUserId, name));

        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateUniqueName_WithSystemDefaultName_ThrowsDomainException()
    {
        // Arrange
        var systemDefaultName = "Salary";
        _mockRepository
            .Setup(r => r.FindByNameAndUserAsync(_testUserId, systemDefaultName))
            .ReturnsAsync((Category?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.ValidateUniqueName(_testUserId, systemDefaultName));

        Assert.Contains("reserved", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateUniqueName_WithEmptyName_ThrowsDomainException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.ValidateUniqueName(_testUserId, ""));

        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanDeleteCategory_WithCustomCategoryAndNoTransactions_ReturnsTrue()
    {
        // Arrange
        var category = new Category(
            CategoryId.New(),
            _testUserId,
            CategoryName.Create("Groceries"),
            CategoryType.Expense,
            ColorHex.Create("#F39C12"),
            "basket");

        // Act & Assert
        Assert.True(_service.CanDeleteCategory(category, hasActiveTransactions: false));
    }

    [Fact]
    public void CanDeleteCategory_WithSystemDefault_ReturnsFalse()
    {
        // Arrange
        var category = Category.CreateSystemDefault(
            CategoryId.New(),
            _testUserId,
            CategoryName.Create("Salary"),
            CategoryType.Income,
            ColorHex.Create("#27AE60"),
            "building-dollar");

        // Act & Assert
        Assert.False(_service.CanDeleteCategory(category, hasActiveTransactions: false));
    }

    [Fact]
    public void CanDeleteCategory_WithActiveTransactions_ReturnsFalse()
    {
        // Arrange
        var category = new Category(
            CategoryId.New(),
            _testUserId,
            CategoryName.Create("Groceries"),
            CategoryType.Expense,
            ColorHex.Create("#F39C12"),
            "basket");

        // Act & Assert
        Assert.False(_service.CanDeleteCategory(category, hasActiveTransactions: true));
    }

    [Fact]
    public void CanDeleteCategory_WithNullCategory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(
            () => _service.CanDeleteCategory(null!, hasActiveTransactions: false));

        Assert.Equal("category", ex.ParamName);
    }

    [Fact]
    public void GetSystemDefaults_ReturnsExactly24Categories()
    {
        // Act
        var defaults = _service.GetSystemDefaults(_testUserId);

        // Assert
        Assert.Equal(24, defaults.Count);
    }

    [Fact]
    public void GetSystemDefaults_ReturnsAllMarkedAsSystemDefault()
    {
        // Act
        var defaults = _service.GetSystemDefaults(_testUserId);

        // Assert
        Assert.All(defaults, c => Assert.True(c.IsSystemDefault));
    }

    [Theory]
    [InlineData(CategoryType.Income, 5)]  // 5 income categories
    [InlineData(CategoryType.Expense, 19)] // 19 expense categories
    public void GetSystemDefaults_HasCorrectCategoryTypeDistribution(CategoryType type, int expectedCount)
    {
        // Act
        var defaults = _service.GetSystemDefaults(_testUserId);

        // Assert
        var count = defaults.Count(c => c.Type == type);
        Assert.Equal(expectedCount, count);
    }

    [Theory]
    [InlineData("Salary")]
    [InlineData("Sales")]
    [InlineData("Investments")]
    [InlineData("Gifts")]
    [InlineData("Other Income")]
    [InlineData("Housing")]
    [InlineData("Utilities")]
    [InlineData("Insurance")]
    [InlineData("Subscriptions")]
    [InlineData("Education")]
    [InlineData("Groceries")]
    [InlineData("Transportation")]
    [InlineData("Personal Care")]
    [InlineData("Home")]
    [InlineData("Pets")]
    [InlineData("Restaurants")]
    [InlineData("Entertainment")]
    [InlineData("Shopping")]
    [InlineData("Travel")]
    [InlineData("Health & Wellness")]
    [InlineData("Debt Payments")]
    [InlineData("Savings & Investment")]
    [InlineData("Donations")]
    [InlineData("Unexpected Expenses")]
    public void GetSystemDefaults_ContainsExpectedCategories(string categoryName)
    {
        // Act
        var defaults = _service.GetSystemDefaults(_testUserId);

        // Assert
        Assert.Contains(defaults, c => c.Name.Value == categoryName);
    }

    [Fact]
    public void GetSystemDefaults_AllCategoriesHaveValidProperties()
    {
        // Act
        var defaults = _service.GetSystemDefaults(_testUserId);

        // Assert
        Assert.All(defaults, c =>
        {
            Assert.NotEqual(Guid.Empty, c.Id.Value);
            Assert.Equal(_testUserId, c.UserId);
            Assert.NotNull(c.Name);
            Assert.NotEmpty(c.Name.Value);
            Assert.NotNull(c.Color);
            Assert.NotEmpty(c.Color.Value);
            Assert.NotNull(c.IconName);
            Assert.NotEmpty(c.IconName);
            Assert.True(c.IsSystemDefault);
        });
    }

    [Fact]
    public void GetSystemDefaults_AllCategoriesHaveValidColors()
    {
        // Act
        var defaults = _service.GetSystemDefaults(_testUserId);

        // Assert
        var colorRegex = new System.Text.RegularExpressions.Regex(@"^#[0-9A-F]{6}$");
        Assert.All(defaults, c =>
        {
            Assert.Matches(colorRegex, c.Color.Value);
        });
    }

    [Fact]
    public void GetSystemDefaults_IncomeAndExpenseGroupsHaveCorrectColors()
    {
        // Act
        var defaults = _service.GetSystemDefaults(_testUserId);

        // Assert Income categories (green)
        var incomeCategories = defaults.Where(c => c.Type == CategoryType.Income);
        Assert.All(incomeCategories, c => Assert.Equal("#27AE60", c.Color.Value));

        // Assert Expense categories by color groups
        var fixedExpenseColors = new[] { "#E74C3C" };
        var variableExpenseColors = new[] { "#F39C12" };
        var lifestyleColors = new[] { "#9B59B6" };
        var financeColors = new[] { "#3498DB" };

        var allValidExpenseColors = fixedExpenseColors
            .Concat(variableExpenseColors)
            .Concat(lifestyleColors)
            .Concat(financeColors)
            .ToList();

        var expenseCategories = defaults.Where(c => c.Type == CategoryType.Expense);
        Assert.All(expenseCategories, c =>
        {
            Assert.Contains(c.Color.Value, allValidExpenseColors);
        });
    }
}
