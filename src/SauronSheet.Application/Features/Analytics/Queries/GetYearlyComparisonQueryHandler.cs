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
/// Handler for GetYearlyComparisonQuery.
/// Compares monthly income and expenses between two years (12 entries each).
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

        // Separate income and expenses by month for Year 1
        var y1IncomeByMonth = year1Transactions
            .Where(t => t.Amount.IsPositive)
            .GroupBy(t => t.Date.GetSpainMonth())
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount.Amount));

        var y1ExpensesByMonth = year1Transactions
            .Where(t => t.Amount.IsNegative)
            .GroupBy(t => t.Date.GetSpainMonth())
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        // Separate income and expenses by month for Year 2
        var y2IncomeByMonth = year2Transactions
            .Where(t => t.Amount.IsPositive)
            .GroupBy(t => t.Date.GetSpainMonth())
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount.Amount));

        var y2ExpensesByMonth = year2Transactions
            .Where(t => t.Amount.IsNegative)
            .GroupBy(t => t.Date.GetSpainMonth())
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        var result = new List<YearlyComparisonDto>();
        for (int month = 1; month <= 12; month++)
        {
            result.Add(new YearlyComparisonDto(
                month,
                CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                y1IncomeByMonth.GetValueOrDefault(month, 0m),
                y1ExpensesByMonth.GetValueOrDefault(month, 0m),
                y2IncomeByMonth.GetValueOrDefault(month, 0m),
                y2ExpensesByMonth.GetValueOrDefault(month, 0m),
                "EUR"));
        }

        return result;
    }
}
