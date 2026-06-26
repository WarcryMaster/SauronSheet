namespace SauronSheet.Application.Features.Analytics.Queries;

using System;
using System.Collections.Generic;
using System.Globalization;
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

/// <summary>
/// Handler for GetMonthlyTrendsQuery.
/// Returns one entry per calendar month overlapping the date range,
/// padding missing months with zeros. Year field populated from Spain-local date.
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
        var dateSpec = new TransactionByDateRangeSpecification(request.FromDate, request.ToDate);
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);

        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        // Group by year+month (Spain-local)
        var byYearMonth = transactions
            .GroupBy(t => new
            {
                Year = t.Date.ToSpainLocal().Year,
                Month = t.Date.ToSpainLocal().Month
            })
            .ToDictionary(g => (g.Key.Year, g.Key.Month), g => g.ToList());

        // Enumerate all calendar months in the range
        var startYear = request.FromDate.Year;
        var startMonth = request.FromDate.Month;
        var endYear = request.ToDate.Year;
        var endMonth = request.ToDate.Month;

        var result = new List<MonthlyTrendDto>();
        int year = startYear;
        int month = startMonth;

        while (year < endYear || (year == endYear && month <= endMonth))
        {
            var monthTransactions = byYearMonth.GetValueOrDefault(
                (year, month), new List<Domain.Entities.Transaction>());

            var income = monthTransactions
                .Where(t => t.Amount.IsPositive)
                .Sum(t => t.Amount.Amount);

            var expenses = monthTransactions
                .Where(t => t.Amount.IsNegative)
                .Sum(t => Math.Abs(t.Amount.Amount));

            result.Add(new MonthlyTrendDto(
                year,
                month,
                CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                expenses,
                income,
                income - expenses,
                "EUR",
                monthTransactions.Count));

            // Advance to next month
            month++;
            if (month > 12)
            {
                month = 1;
                year++;
            }
        }

        return result;
    }
}
