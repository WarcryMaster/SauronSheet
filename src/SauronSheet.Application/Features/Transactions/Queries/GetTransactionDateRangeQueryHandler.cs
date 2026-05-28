namespace SauronSheet.Application.Features.Transactions.Queries;

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SauronSheet.Domain.Common;
using Domain.Repositories;
using Domain.ValueObjects;

/// <summary>
/// Handler for GetTransactionDateRangeQuery.
/// </summary>
public class GetTransactionDateRangeQueryHandler
    : IRequestHandler<GetTransactionDateRangeQuery, (DateTime MinDate, DateTime MaxDate)?>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;

    public GetTransactionDateRangeQueryHandler(
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<(DateTime MinDate, DateTime MaxDate)?> Handle(
        GetTransactionDateRangeQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);
        return await _transactionRepo.GetDateRangeAsync(userId);
    }
}
