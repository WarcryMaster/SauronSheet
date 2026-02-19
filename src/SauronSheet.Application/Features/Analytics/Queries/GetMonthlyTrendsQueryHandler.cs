namespace SauronSheet.Application.Features.Analytics.Queries;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Domain.Repositories;
using Domain.Specifications;
using Domain.ValueObjects;
using DTOs;
using MediatR;

/// <summary>
/// Handler for GetMonthlyTrendsQuery.
/// Returns 12 monthly entries with income, expenses and net for a year.
/// Phase 4 (US3).
/// </summary>
public class GetMonthlyTrendsQueryHandler
    : IRequestHandler<GetMonthlyTrendsQuery, List<MonthlyTrendDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;

    public GetMonthlyTrendsQueryHandler(
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<List<MonthlyTrendDto>> Handle(
        GetMonthlyTrendsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        var userSpec = new TransactionByUserSpecification(userId);
        var dateSpec = new TransactionByDateRangeSpecification(
            new DateTime(request.Year, 1, 1),
            new DateTime(request.Year, 12, 31, 23, 59, 59));
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);

        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        var byMonth = transactions.GroupBy(t => t.Date.Month)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<MonthlyTrendDto>();
        for (int month = 1; month <= 12; month++)
        {
            var monthTransactions = byMonth.GetValueOrDefault(month, new List<Domain.Entities.Transaction>());

            var income = monthTransactions
                .Where(t => t.Amount.IsPositive)
                .Sum(t => t.Amount.Amount);

            var expenses = monthTransactions
                .Where(t => t.Amount.IsNegative)
                .Sum(t => Math.Abs(t.Amount.Amount));

            result.Add(new MonthlyTrendDto(
                month,
                CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                expenses,
                income,
                income - expenses,
                "EUR",
                monthTransactions.Count));
        }

        return result;
    }
}
