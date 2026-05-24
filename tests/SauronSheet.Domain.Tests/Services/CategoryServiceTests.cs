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
    public async Task ValidateUniqueName_WithPreviouslySystemDefaultName_NoLongerThrows()
    {
        // Arrange
        var previouslyReservedName = "Salary";
        _mockRepository
            .Setup(r => r.FindByNameAndUserAsync(_testUserId, previouslyReservedName))
            .ReturnsAsync((Category?)null);

        // Act & Assert - System defaults removed, so "Salary" is no longer reserved
        await _service.ValidateUniqueName(_testUserId, previouslyReservedName);
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

}
