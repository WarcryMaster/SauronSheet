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
/// Handler for GetRecentTransactionsQuery.
/// Returns the N most recent transactions ordered by date descending.
/// Phase 4 (US5).
/// DT-1b/DT-1c: SubcategoryName populated via single batch fetch (no N+1).
/// </summary>
public class GetRecentTransactionsQueryHandler
    : IRequestHandler<GetRecentTransactionsQuery, List<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ISubcategoryRepository _subcategoryRepo;
    private readonly IUserContext _userContext;

    public GetRecentTransactionsQueryHandler(
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

    public async Task<List<TransactionDto>> Handle(
        GetRecentTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        var userSpec = new TransactionByUserSpecification(userId);
        var allTransactions = await _transactionRepo.FindBySpecificationAsync(userSpec);

        // Load categories for name lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name.Value);

        // DT-1b: batch-fetch subcategories once; build in-memory dict to avoid N+1.
        // TryGetValue used at mapping time — null-safe for DT-1c (SubcategoryId == null).
        var subcategories = await _subcategoryRepo.GetByUserIdAsync(userId);
        var subcategoryLookup = subcategories.ToDictionary(s => s.Id, s => s.Name.Value);

        var recent = allTransactions
            .OrderByDescending(t => t.Date)
            .Take(request.Count)
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

        return recent;
    }
}
