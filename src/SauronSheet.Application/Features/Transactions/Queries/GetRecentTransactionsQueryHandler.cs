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
/// </summary>
public class GetRecentTransactionsQueryHandler
    : IRequestHandler<GetRecentTransactionsQuery, List<TransactionDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetRecentTransactionsQueryHandler(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
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
                t.CategoryId != null && categoryLookup.ContainsKey(t.CategoryId)
                    ? categoryLookup[t.CategoryId]
                    : null,
                t.ImportedFrom,
                t.CreatedAt))
            .ToList();

        return recent;
    }
}
