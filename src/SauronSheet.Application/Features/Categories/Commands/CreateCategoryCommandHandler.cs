namespace SauronSheet.Application.Features.Categories.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Entities;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using MediatR;
using Services;

public class CreateCategoryCommandHandler
    : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly CategoryService _categoryService;
    private readonly IUserContext _userContext;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepo,
        CategoryService categoryService,
        IUserContext userContext)
    {
        _categoryRepo = categoryRepo;
        _categoryService = categoryService;
        _userContext = userContext;
    }

    public async Task<Guid> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Feature 3: Check database for duplicate (across all scopes: user + system)
        var duplicate = await _categoryRepo.FindByNameAsync(request.Name);
        if (duplicate != null)
            throw new Domain.Exceptions.DomainException($"Category name '{request.Name}' is already in use (system or custom)");

        // Validate unique name via domain service (hardcoded + cached system defaults)
        await _categoryService.ValidateUniqueName(userId, request.Name);

        var categoryId = new CategoryId(Guid.NewGuid());
        var categoryName = CategoryName.Create(request.Name);
        var categoryColor = ColorHex.Create(request.Color ?? "#3498DB"); // Default color if not provided
        var iconName = request.Icon ?? "tag"; // Default icon if not provided

        var category = new Category(
            categoryId,
            userId,
            categoryName,
            request.Type,
            categoryColor,
            iconName);

        // Compute the normalized deduplication key (single source of truth: CategoryNormalizer).
        // This key is stored in the normalized_name column (NOT NULL after migration 011).
        var normalizedName = CategoryNormalizer.Normalize(request.Name)
            ?? throw new Domain.Exceptions.DomainException("Category name cannot be normalized to a valid key.");

        await _categoryRepo.AddAsync(category, normalizedName);
        return categoryId.Value;
    }
}
