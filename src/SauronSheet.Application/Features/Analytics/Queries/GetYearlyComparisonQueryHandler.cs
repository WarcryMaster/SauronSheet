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
/// Handler for GetYearlyComparisonQuery.
/// Compares monthly expenses between two years (12 entries).
/// Phase 4 (US4).
/// </summary>
public class GetYearlyComparisonQueryHandler
    : IRequestHandler<GetYearlyComparisonQuery, List<YearlyComparisonDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IUserContext _userContext;

    public GetYearlyComparisonQueryHandler(
        ITransactionRepository transactionRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _userContext = userContext;
    }

    public async Task<List<YearlyComparisonDto>> Handle(
        GetYearlyComparisonQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        // Load Year 1
        var year1Spec = CompositeSpecification<Domain.Entities.Transaction>.And(
            new TransactionByUserSpecification(userId),
            new TransactionByDateRangeSpecification(
                new DateTime(request.Year1, 1, 1),
                new DateTime(request.Year1, 12, 31, 23, 59, 59)));
        var year1Transactions = await _transactionRepo.FindBySpecificationAsync(year1Spec);

        // Load Year 2
        var year2Spec = CompositeSpecification<Domain.Entities.Transaction>.And(
            new TransactionByUserSpecification(userId),
            new TransactionByDateRangeSpecification(
                new DateTime(request.Year2, 1, 1),
                new DateTime(request.Year2, 12, 31, 23, 59, 59)));
        var year2Transactions = await _transactionRepo.FindBySpecificationAsync(year2Spec);

        // Only expenses
        var y1ByMonth = year1Transactions
            .Where(t => t.Amount.IsNegative)
            .GroupBy(t => t.Date.Month)
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        var y2ByMonth = year2Transactions
            .Where(t => t.Amount.IsNegative)
            .GroupBy(t => t.Date.Month)
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        var result = new List<YearlyComparisonDto>();
        for (int month = 1; month <= 12; month++)
        {
            var y1Amount = y1ByMonth.GetValueOrDefault(month, 0m);
            var y2Amount = y2ByMonth.GetValueOrDefault(month, 0m);
            var difference = y2Amount - y1Amount;
            decimal? percentageChange = y1Amount != 0
                ? Math.Round((difference / y1Amount) * 100, 2)
                : null;

            result.Add(new YearlyComparisonDto(
                month,
                CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                y1Amount,
                y2Amount,
                difference,
                percentageChange,
                "EUR"));
        }

        return result;
    }
}
