namespace SauronSheet.Application.Features.Categories.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Entities;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using MediatR;

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

        // Validate unique name via domain service
        await _categoryService.ValidateUniqueName(userId, request.Name);

        var categoryId = new CategoryId(Guid.NewGuid());
        var category = new Category(categoryId, userId, request.Name, request.Color, request.Icon);

        await _categoryRepo.AddAsync(category);
        return categoryId.Value;
    }
}
