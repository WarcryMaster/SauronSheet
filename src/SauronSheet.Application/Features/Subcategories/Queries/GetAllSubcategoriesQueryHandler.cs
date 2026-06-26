namespace SauronSheet.Application.Features.Subcategories.Queries;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Repositories;
using Domain.ValueObjects;
using DTOs;
using MediatR;

/// <summary>
/// Returns all subcategories for the current user.
/// Used by forms that need to populate dependent category/subcategory dropdowns.
/// </summary>
public class GetAllSubcategoriesQueryHandler
    : IRequestHandler<GetAllSubcategoriesQuery, List<SubcategoryDto>>
{
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly IUserContext _userContext;

    public GetAllSubcategoriesQueryHandler(
        ISubcategoryRepository subcategoryRepo,
        IUserContext userContext)
    {
        _subcategoryRepo = subcategoryRepo;
        _userContext = userContext;
    }

    public async Task<List<SubcategoryDto>> Handle(
        GetAllSubcategoriesQuery request,
        CancellationToken cancellationToken)
    {
        UserId userId = new UserId(_userContext.UserId);

        IReadOnlyList<Domain.Entities.Subcategory> subcategories =
            await _subcategoryRepo.GetByUserIdAsync(userId);

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
