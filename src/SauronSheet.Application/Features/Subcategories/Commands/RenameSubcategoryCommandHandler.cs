namespace SauronSheet.Application.Features.Subcategories.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Application.Services;
using SauronSheet.Domain.Common;
using Domain.Repositories;
using Domain.ValueObjects;
using Domain.Exceptions;
using MediatR;

public class RenameSubcategoryCommandHandler
    : IRequestHandler<RenameSubcategoryCommand, Unit>
{
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public RenameSubcategoryCommandHandler(
        ISubcategoryRepository subcategoryRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _subcategoryRepo = subcategoryRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(
        RenameSubcategoryCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var subcategoryId = new SubcategoryId(request.SubcategoryId);

        var subcategory = await _subcategoryRepo.GetByIdAsync(subcategoryId);
        if (subcategory == null)
            throw new EntityNotFoundException("Subcategory", request.SubcategoryId);

        var category = await _categoryRepo.GetByIdAsync(subcategory.CategoryId);
        if (category == null || category.UserId != userId)
            throw new EntityNotFoundException("Subcategory", request.SubcategoryId);

        var duplicate = await _subcategoryRepo.FindByNameAsync(userId, subcategory.CategoryId, request.NewName);
        if (duplicate != null && duplicate.Id != subcategoryId)
            throw new DomainException($"Subcategory name '{request.NewName}' already exists in this category.");

        subcategory.Update(SubcategoryName.Create(request.NewName));

        var normalizedName = CategoryNormalizer.Normalize(request.NewName)
            ?? throw new DomainException("Subcategory name cannot be empty.");

        await _subcategoryRepo.UpdateAsync(subcategory, normalizedName);
        return Unit.Value;
    }
}
