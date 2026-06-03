namespace SauronSheet.Application.Features.Subcategories.Queries;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Repositories;
using Domain.ValueObjects;
using Domain.Exceptions;
using DTOs;
using MediatR;

public class GetSubcategoriesByCategoryQueryHandler
    : IRequestHandler<GetSubcategoriesByCategoryQuery, List<SubcategoryDto>>
{
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetSubcategoriesByCategoryQueryHandler(
        ISubcategoryRepository subcategoryRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _subcategoryRepo = subcategoryRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<List<SubcategoryDto>> Handle(
        GetSubcategoriesByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var categoryId = new CategoryId(request.CategoryId);

        var category = await _categoryRepo.GetByIdAsync(categoryId);
        if (category == null)
            throw new EntityNotFoundException("Category", request.CategoryId);

        if (category.UserId != userId)
            throw new EntityNotFoundException("Category", request.CategoryId);

        var subcategories = await _subcategoryRepo.GetByCategoryIdAsync(categoryId);

        return subcategories
            .Select(s => new SubcategoryDto(
                s.Id.Value,
                s.CategoryId.Value,
                s.Name.Value,
                s.IsAutoCreated))
            .OrderBy(s => s.Name)
            .ToList();
    }
}
