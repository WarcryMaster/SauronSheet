namespace SauronSheet.Application.Features.Subcategories.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Domain.Exceptions;
using MediatR;
using Services;

public class CreateSubcategoryCommandHandler
    : IRequestHandler<CreateSubcategoryCommand, Guid>
{
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public CreateSubcategoryCommandHandler(
        ISubcategoryRepository subcategoryRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _subcategoryRepo = subcategoryRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<Guid> Handle(
        CreateSubcategoryCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var categoryId = new CategoryId(request.CategoryId);

        var category = await _categoryRepo.GetByIdAsync(categoryId);
        if (category == null)
            throw new EntityNotFoundException("Category", request.CategoryId);

        if (category.UserId != userId)
            throw new EntityNotFoundException("Category", request.CategoryId);

        // Normalize first — the UNIQUE constraint is on normalized_name,
        // so the duplicate check must use the same key the DB enforces.
        var normalizedName = CategoryNormalizer.Normalize(request.Name)
            ?? throw new DomainException("Subcategory name cannot be normalized to a valid key.");

        var duplicate = await _subcategoryRepo.FindByNormalizedNameAsync(userId, categoryId, normalizedName);
        if (duplicate != null)
            throw new DomainException($"Subcategory name '{request.Name}' already exists in this category.");

        var subcategoryId = new SubcategoryId(Guid.NewGuid());
        var subcategoryName = SubcategoryName.Create(request.Name);

        var subcategory = new Subcategory(
            subcategoryId,
            userId,
            categoryId,
            subcategoryName,
            isAutoCreated: false);

        await _subcategoryRepo.AddAsync(subcategory, normalizedName);
        return subcategoryId.Value;
    }
}
