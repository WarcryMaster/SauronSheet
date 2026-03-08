namespace SauronSheet.Domain.Tests.Services;

using System;
using System.Collections.Generic;
using Xunit;
using Moq;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Feature 3: Unit tests for CategoryService caching behavior.
/// Tests lazy singleton caching and NULL UserId semantics.
/// </summary>
[Trait("Category", "Domain")]
public class CategoryServiceCachingTests
{
    private readonly Mock<ICategoryRepository> _mockRepository;
    private readonly CategoryService _service;

    public CategoryServiceCachingTests()
    {
        _mockRepository = new Mock<ICategoryRepository>();
        _service = new CategoryService(_mockRepository.Object);
    }

    // T-3.13: GetSystemDefaults_CreatesWithNullUserId
    [Fact]
    public void GetSystemDefaults_CreatesWithNullUserId()
    {
        // Act
        var defaults = _service.GetSystemDefaults();

        // Assert
        Assert.Equal(24, defaults.Count);
        Assert.All(defaults, category =>
        {
            Assert.Null(category.UserId);
            Assert.True(category.IsSystemDefault);
        });
    }

    // T-3.14: GetSystemDefaults_CachedAfterFirstCall
    [Fact]
    public void GetSystemDefaults_CachedAfterFirstCall()
    {
        // Act
        var defaults1 = _service.GetSystemDefaults();
        var defaults2 = _service.GetSystemDefaults();

        // Assert - Same instance (proves caching)
        Assert.Same(defaults1, defaults2);
    }

    // T-3.13 & T-3.14 combined: Verify all 24 categories exist with correct names
    [Fact]
    public void GetSystemDefaults_ContainsExpected24Categories()
    {
        // Act
        var defaults = _service.GetSystemDefaults();

        // Assert
        var categoryNames = new[]
        {
            // Income (5)
            "Salary", "Sales", "Investments", "Gifts", "Other Income",
            // Fixed Expenses (5)
            "Housing", "Utilities", "Insurance", "Subscription", "Education",
            // Variable Expenses (5)
            "Groceries", "Transportation", "Entertainment", "Dining Out", "Shopping",
            // Lifestyle (5)
            "Coffee", "Fitness", "Healthcare", "Hobbies", "Gifts Given",
            // Finance & Other (4)
            "Phone", "Internet", "Gas", "Other Expense"
        };

        Assert.Equal(24, defaults.Count);
        foreach (var expectedName in categoryNames)
        {
            Assert.Contains(defaults, c => c.Name.Value == expectedName);
        }
    }

    // Verify income vs expense split
    [Fact]
    public void GetSystemDefaults_ContainsIncomeAndExpenseCategories()
    {
        // Act
        var defaults = _service.GetSystemDefaults();

        // Assert
        var incomeCategories = defaults.Where(c => c.Type == CategoryType.Income).ToList();
        var expenseCategories = defaults.Where(c => c.Type == CategoryType.Expense).ToList();

        Assert.Equal(5, incomeCategories.Count);
        Assert.Equal(19, expenseCategories.Count);
    }
}
