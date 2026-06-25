namespace SauronSheet.Application.Features.Transactions.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SauronSheet.Domain.Common;
using Domain.Repositories;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;
using SauronSheet.Application.Helpers;

public class GetTransactionsQueryHandler
    : IRequestHandler<GetTransactionsQuery, PaginatedResultDto<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly IUserContext _userContext;

    public GetTransactionsQueryHandler(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        ISubcategoryRepository subcategoryRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _subcategoryRepo = subcategoryRepo;
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

        if (!string.IsNullOrEmpty(request.ImportedFrom))
        {
            // Support comma-separated multiple sources
            var sources = request.ImportedFrom
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (sources.Length == 1)
            {
                var sourceSpec = new TransactionByImportedFromSpecification(sources[0]);
                spec = CompositeSpecification<Domain.Entities.Transaction>.And(spec, sourceSpec);
            }
            else if (sources.Length > 1)
            {
                var sourceSpec = new TransactionByMultipleImportedFromsSpecification(sources);
                spec = CompositeSpecification<Domain.Entities.Transaction>.And(spec, sourceSpec);
            }
        }

        var filtered = await _transactionRepo.FindBySpecificationAsync(spec);

        // Sort by date descending
        var sorted = filtered.OrderByDescending(t => t.Date);

        // Get total count
        var totalCount = sorted.Count();

        // Apply pagination
        var skip = (request.PageNumber - 1) * request.PageSize;
        var paginated = sorted.Skip(skip).Take(request.PageSize).ToList();

        // DT-1d: batch-fetch categories once; build in-memory dict to avoid N+1.
        // Identical pattern to GetRecentTransactionsQueryHandler (L51-52).
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name.Value);

        // DT-1b: batch-fetch subcategories once; build in-memory dict to avoid N+1.
        // TryGetValue used at mapping time — null-safe for DT-1c (SubcategoryId == null).
        var subcategories = await _subcategoryRepo.GetByUserIdAsync(userId);
        var subcategoryLookup = subcategories.ToDictionary(s => s.Id, s => s.Name.Value);

        var dtos = paginated.Select(t => new TransactionDto(
            t.Id.Value,
            t.Amount.Amount,
            t.Amount.Currency,
            t.Date.ToSpainLocal(),
            t.Description,
            t.CategoryId?.Value,
            t.CategoryId is CategoryId catId && categoryLookup.TryGetValue(catId, out var catName)
                ? catName
                : null,
            t.ImportedFrom,
            t.CreatedAt,
            BankCategory: t.BankCategory,
            BankSubcategory: t.BankSubcategory,
            SubcategoryId: t.SubcategoryId?.Value.ToString(),
            SubcategoryName: t.SubcategoryId != null && subcategoryLookup.TryGetValue(t.SubcategoryId, out var subName)
                ? subName
                : null,
            CategorySource: t.CategorySource.ToString()
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
