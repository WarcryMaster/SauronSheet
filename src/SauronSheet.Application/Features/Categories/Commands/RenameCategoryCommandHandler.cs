namespace SauronSheet.Application.Features.Categories.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using Domain.Exceptions;
using MediatR;

public class RenameCategoryCommandHandler
    : IRequestHandler<RenameCategoryCommand, Unit>
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly CategoryService _categoryService;
    private readonly IUserContext _userContext;

    public RenameCategoryCommandHandler(
        ICategoryRepository categoryRepo,
        CategoryService categoryService,
        IUserContext userContext)
    {
        _categoryRepo = categoryRepo;
        _categoryService = categoryService;
        _userContext = userContext;
    }

    public async Task<Unit> Handle(
        RenameCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var categoryId = new CategoryId(request.CategoryId);

        var category = await _categoryRepo.GetByIdAsync(categoryId);
        if (category == null)
            throw new EntityNotFoundException("Category", request.CategoryId);

        // Tenant scoping
        if (category.UserId != userId)
            throw new EntityNotFoundException("Category", request.CategoryId);

        // Validate unique name
        await _categoryService.ValidateUniqueName(userId, request.NewName);

        // Rename via domain method (guards enforced)
        category.Rename(request.NewName);

        await _categoryRepo.UpdateAsync(category);
        return Unit.Value;
    }
}
