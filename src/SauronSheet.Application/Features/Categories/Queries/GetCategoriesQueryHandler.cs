namespace SauronSheet.Application.Features.Categories.Queries;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Repositories;
using Domain.ValueObjects;
using DTOs;
using MediatR;

public class GetCategoriesQueryHandler
    : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;

    public GetCategoriesQueryHandler(
        ICategoryRepository categoryRepo,
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _categoryRepo = categoryRepo;
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<List<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        var categories = await _categoryRepo.GetByUserIdAsync(userId);

        var categoryIds = categories.Select(c => c.Id).ToList();
        var counts = await _transactionRepo.GetCountsByCategoriesAsync(categoryIds);

        var result = categories.Select(c => new CategoryDto(
            c.Id.Value,
            c.Name.Value,
            c.Type.ToString(),
            c.Color.Value,
            c.IconName,
            c.CreatedAt,
            c.UpdatedAt ?? DateTime.UtcNow,
            TransactionCount: counts.GetValueOrDefault(c.Id, 0)
        )).ToList();

        // Sort alphabetically by name (system defaults removed in Chunk 3)
        return result
            .OrderBy(c => c.Name)
            .ToList();
    }
}
