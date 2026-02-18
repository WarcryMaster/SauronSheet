namespace SauronSheet.Application.Features.Transactions.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Repositories;
using Domain.ValueObjects;
using DTOs;
using MediatR;

public class GetTransactionsQueryHandler
    : IRequestHandler<GetTransactionsQuery, PaginatedResultDto<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetTransactionsQueryHandler(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<PaginatedResultDto<TransactionDto>> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // TODO Phase 4: Apply specifications for filtering
        var allTransactions = await _transactionRepo.GetByUserIdAsync(userId);

        // Apply filters manually for Phase 3
        var filtered = allTransactions.AsQueryable();

        if (request.CategoryId.HasValue)
        {
            var categoryId = new CategoryId(request.CategoryId.Value);
            filtered = filtered.Where(t => t.CategoryId == categoryId);
        }

        if (request.StartDate.HasValue)
            filtered = filtered.Where(t => t.Date >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            filtered = filtered.Where(t => t.Date <= request.EndDate.Value);

        // Sort by date descending
        var sorted = filtered.OrderByDescending(t => t.Date);

        // Get total count
        var totalCount = sorted.Count();

        // Apply pagination
        var skip = (request.PageNumber - 1) * request.PageSize;
        var paginated = sorted.Skip(skip).Take(request.PageSize).ToList();

        // Map to DTOs
        var categoryIds = paginated
            .Where(t => t.CategoryId != null)
            .Select(t => t.CategoryId!.Value)
            .Distinct()
            .ToList();

        var categories = new Dictionary<CategoryId, string>();
        foreach (var catId in categoryIds)
        {
            var category = await _categoryRepo.GetByIdAsync(new CategoryId(catId));
            if (category != null)
                categories[new CategoryId(catId)] = category.Name;
        }

        var dtos = paginated.Select(t => new TransactionDto(
            t.Id.Value,
            t.Amount.Amount,
            t.Amount.Currency,
            t.Date,
            t.Description,
            t.CategoryId?.Value,
            t.CategoryId != null && categories.ContainsKey(t.CategoryId)
                ? categories[t.CategoryId]
                : null,
            t.ImportedFrom,
            t.CreatedAt
        )).ToList();

        // CLARIFICATION A-4: TotalPages calculation
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PaginatedResultDto<TransactionDto>(
            dtos,
            totalCount,
            request.PageNumber,
            request.PageSize,
            totalPages);
    }
}
