namespace SauronSheet.Application.Features.Analytics.Queries;

using System;
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
/// Handler for GetTransactionSummaryQuery.
/// Calculates income, expenses, net amount and count for a date range.
/// Phase 4 (US6).
/// </summary>
public class GetTransactionSummaryQueryHandler
    : IRequestHandler<GetTransactionSummaryQuery, TransactionSummaryDto>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;

    public GetTransactionSummaryQueryHandler(
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<TransactionSummaryDto> Handle(
        GetTransactionSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        var userSpec = new TransactionByUserSpecification(userId);
        var dateSpec = new TransactionByDateRangeSpecification(request.FromDate, request.ToDate);
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);

        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        var totalIncome = transactions
            .Where(t => t.Amount.IsPositive)
            .Sum(t => t.Amount.Amount);

        var totalExpenses = transactions
            .Where(t => t.Amount.IsNegative)
            .Sum(t => Math.Abs(t.Amount.Amount));

        var netAmount = totalIncome - totalExpenses;

        return new TransactionSummaryDto(
            totalIncome,
            totalExpenses,
            netAmount,
            transactions.Count,
            "EUR",
            request.FromDate,
            request.ToDate);
    }
}
