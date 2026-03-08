namespace SauronSheet.Application.Features.Transactions.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Repositories;
using Domain.Specifications;
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

        // Build composed specification from filters
        ISpecification<Domain.Entities.Transaction> spec = new TransactionByUserSpecification(userId);

        if (request.CategoryId.HasValue)
        {
            var categorySpec = new TransactionByCategorySpecification(new CategoryId(request.CategoryId.Value));
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(spec, categorySpec);
        }

        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            var dateSpec = new TransactionByDateRangeSpecification(request.StartDate.Value, request.EndDate.Value);
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(spec, dateSpec);
        }
        else if (request.StartDate.HasValue)
        {
            var dateSpec = new TransactionByDateRangeSpecification(request.StartDate.Value, DateTime.MaxValue);
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(spec, dateSpec);
        }
        else if (request.EndDate.HasValue)
        {
            var dateSpec = new TransactionByDateRangeSpecification(DateTime.MinValue, request.EndDate.Value);
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(spec, dateSpec);
        }

        var filtered = await _transactionRepo.FindBySpecificationAsync(spec);

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
                categories[new CategoryId(catId)] = category.Name.Value;
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
