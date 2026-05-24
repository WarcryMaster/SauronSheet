namespace SauronSheet.Domain.Entities;

using System;
using ValueObjects;
using Common;

/// <summary>
/// Subcategory aggregate root.
/// Represents a sub-level classification within a category.
/// Can be user-created or auto-created by the resolution service.
/// </summary>
public class Subcategory : AggregateRoot<SubcategoryId>
{
    public UserId? UserId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public SubcategoryName Name { get; private set; }
    public bool IsAutoCreated { get; private set; }

    public Subcategory(
        SubcategoryId id,
        UserId? userId,
        CategoryId categoryId,
        SubcategoryName name,
        bool isAutoCreated)
        : base(id)
    {
        CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        UserId = userId;
        IsAutoCreated = isAutoCreated;
    }
}
