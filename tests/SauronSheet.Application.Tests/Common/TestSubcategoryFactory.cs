namespace SauronSheet.Application.Tests.Common;

using SauronSheet.Domain.Entities;
using SauronSheet.Domain.ValueObjects;

public static class TestSubcategoryFactory
{
    public static Subcategory CreateUserSubcategory(
        SubcategoryId? subcategoryId = null,
        UserId? userId = null,
        CategoryId? categoryId = null,
        string? name = null,
        bool isAutoCreated = false)
    {
        return new Subcategory(
            subcategoryId ?? SubcategoryId.New(),
            userId ?? new UserId("test-user-id"),
            categoryId ?? CategoryId.New(),
            SubcategoryName.Create(name ?? "Test Subcategory"),
            isAutoCreated);
    }
}
