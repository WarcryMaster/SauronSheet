namespace SauronSheet.Application.Features.Categories.Queries;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Repositories;
using Domain.ValueObjects;
using DTOs;
using MediatR;
using Commands;

public class GetCategoriesQueryHandler
    : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;
    private readonly IMediator _mediator;

    public GetCategoriesQueryHandler(
        ICategoryRepository categoryRepo,
        ITransactionRepository transactionRepo,
        IUserContext userContext,
        IMediator mediator)
    {
        _categoryRepo = categoryRepo;
        _transactionRepo = transactionRepo;
        _userContext = userContext;
        _mediator = mediator;
    }

    public async Task<List<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // CLARIFICATION A-1: Seed system defaults via MediatR (idempotent)
        await _mediator.Send(new SeedSystemDefaultsCommand(), cancellationToken);

        var categories = await _categoryRepo.GetByUserIdAsync(userId);

        // CRITICAL FIX I-4: Calculate TransactionCount per category
        var categoryIds = categories.Select(c => c.Id).ToList();
        var transactionCounts = await _transactionRepo.GetCountsByCategoriesAsync(categoryIds);

        var result = categories.Select(c => new CategoryDto(
            c.Id.Value,
            c.Name,
            c.Color,
            c.Icon,
            c.IsSystemDefault,
            transactionCounts.TryGetValue(c.Id, out var count) ? count : 0
        )).ToList();

        // Sort: system defaults first, then user-defined alphabetically
        return result
            .OrderByDescending(c => c.IsSystemDefault)
            .ThenBy(c => c.Name)
            .ToList();
    }
}
