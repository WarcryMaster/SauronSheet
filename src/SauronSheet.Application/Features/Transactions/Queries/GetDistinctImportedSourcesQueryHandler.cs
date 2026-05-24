namespace SauronSheet.Application.Features.Transactions.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Repositories;
using Domain.Specifications;
using Domain.ValueObjects;
using Domain.Common;
using MediatR;

/// <summary>
/// Handler for GetDistinctImportedSourcesQuery.
/// Fetches user transactions and extracts distinct, non-null ImportedFrom values
/// sorted alphabetically.
/// </summary>
public class GetDistinctImportedSourcesQueryHandler
    : IRequestHandler<GetDistinctImportedSourcesQuery, List<string>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;

    public GetDistinctImportedSourcesQueryHandler(
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<List<string>> Handle(
        GetDistinctImportedSourcesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        var spec = new TransactionByUserSpecification(userId);
        var transactions = await _transactionRepo.FindBySpecificationAsync(spec);

        return transactions
            .Select(t => t.ImportedFrom)
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToList();
    }
}
