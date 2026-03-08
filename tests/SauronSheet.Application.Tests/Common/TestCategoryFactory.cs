namespace SauronSheet.Application.Tests.Common;

using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Factory for creating test Category instances with sensible defaults.
/// </summary>
public static class TestCategoryFactory
{
    /// <summary>
    /// Create a user-defined Category with default values for testing.
    /// </summary>
    public static Category CreateUserCategory(
        CategoryId? categoryId = null,
        UserId? userId = null,
        string? name = null,
        CategoryType? type = null,
        string? color = null,
        string? icon = null)
    {
        return new Category(
            categoryId ?? CategoryId.New(),
            userId ?? new UserId("test-user-id"),
            CategoryName.Create(name ?? "Test Category"),
            type ?? CategoryType.Expense,
            ColorHex.Create(color ?? "#3498DB"),
            icon ?? "tag");
    }

    /// <summary>
    /// Create a system default Category with default values for testing.
    /// </summary>
    public static Category CreateSystemCategory(
        CategoryId? categoryId = null,
        UserId? userId = null,
        string? name = null,
        CategoryType? type = null,
        string? color = null,
        string? icon = null)
    {
        return Category.CreateSystemDefault(
            categoryId ?? CategoryId.New(),
            userId ?? new UserId("test-user-id"),
            CategoryName.Create(name ?? "System Category"),
            type ?? CategoryType.Expense,
            ColorHex.Create(color ?? "#3498DB"),
            icon ?? "tag");
    }
}
