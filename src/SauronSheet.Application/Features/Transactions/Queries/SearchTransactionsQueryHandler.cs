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

/// <summary>
/// Handler for SearchTransactionsQuery.
/// Composes specifications dynamically based on provided filters.
/// Phase 4 (US5).
/// DT-1b/DT-1c: SubcategoryName populated via single batch fetch (no N+1).
/// </summary>
public class SearchTransactionsQueryHandler
    : IRequestHandler<SearchTransactionsQuery, PaginatedResultDto<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly IUserContext _userContext;

    public SearchTransactionsQueryHandler(
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
        SearchTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Start with user spec as base
        ISpecification<Domain.Entities.Transaction> spec = new TransactionByUserSpecification(userId);

        // Compose additional filters
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(
                spec, new TransactionByDescriptionKeywordSpecification(request.Keyword));
        }

        if (request.FromDate.HasValue && request.ToDate.HasValue)
        {
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(
                spec, new TransactionByDateRangeSpecification(request.FromDate.Value, request.ToDate.Value));
        }
        else if (request.FromDate.HasValue)
        {
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(
                spec, new TransactionByDateRangeSpecification(request.FromDate.Value, DateTime.MaxValue));
        }
        else if (request.ToDate.HasValue)
        {
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(
                spec, new TransactionByDateRangeSpecification(DateTime.MinValue, request.ToDate.Value));
        }

        if (request.CategoryId.HasValue)
        {
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(
                spec, new TransactionByCategorySpecification(new CategoryId(request.CategoryId.Value)));
        }

        if (request.MinAmount.HasValue || request.MaxAmount.HasValue)
        {
            var min = new Money(request.MinAmount ?? decimal.MinValue, "EUR");
            var max = new Money(request.MaxAmount ?? decimal.MaxValue, "EUR");
            spec = CompositeSpecification<Domain.Entities.Transaction>.And(
                spec, new TransactionByAmountRangeSpecification(min, max));
        }

        var transactions = await _transactionRepo.FindBySpecificationAsync(spec);

        // Load categories for name lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name.Value);

        // DT-1b: batch-fetch subcategories once; build in-memory dict to avoid N+1.
        // TryGetValue used at mapping time — null-safe for DT-1c (SubcategoryId == null).
        var subcategories = await _subcategoryRepo.GetByUserIdAsync(userId);
        var subcategoryLookup = subcategories.ToDictionary(s => s.Id, s => s.Name.Value);

        // Sort, paginate, map
        var sorted = transactions.OrderByDescending(t => t.Date).ToList();
        var totalCount = sorted.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var paginated = sorted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TransactionDto(
                t.Id.Value,
                t.Amount.Amount,
                t.Amount.Currency,
                t.Date,
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
                CategorySource: t.CategorySource.ToString()))
            .ToList();

        return new PaginatedResultDto<TransactionDto>(
            paginated,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages);
    }
}
