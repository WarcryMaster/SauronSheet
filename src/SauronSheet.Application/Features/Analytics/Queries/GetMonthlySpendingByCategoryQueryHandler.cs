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
/// Handler for GetMonthlySpendingByCategoryQuery.
/// Returns monthly spending broken down by category for stacked area charts.
/// Categories are sorted by total amount descending (deterministic legend order).
/// </summary>
public class GetMonthlySpendingByCategoryQueryHandler
    : IRequestHandler<GetMonthlySpendingByCategoryQuery, List<MonthlyCategorySpendingDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetMonthlySpendingByCategoryQueryHandler(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<List<MonthlyCategorySpendingDto>> Handle(
        GetMonthlySpendingByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        var userSpec = new TransactionByUserSpecification(userId);
        var dateSpec = new TransactionByDateRangeSpecification(request.FromDate, request.ToDate);
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);

        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        // Only expenses
        var expenses = transactions.Where(t => t.Amount.IsNegative).ToList();
        if (!expenses.Any())
            return new List<MonthlyCategorySpendingDto>();

        // Load categories for name lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c);

        // Group by year+month (Spain-local) and category
        var grouped = expenses
            .GroupBy(t => new
            {
                Year = t.Date.ToSpainLocal().Year,
                Month = t.Date.ToSpainLocal().Month,
                CategoryId = t.CategoryId
            })
            .Select(g =>
            {
                var amount = g.Sum(t => Math.Abs(t.Amount.Amount));
                var catId = g.Key.CategoryId;
                var catName = "Uncategorized";

                if (catId != null && categoryLookup.TryGetValue(catId, out var cat))
                {
                    catName = cat.Name.Value;
                }

                return new MonthlyCategorySpendingDto(
                    g.Key.Year,
                    g.Key.Month,
                    CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(g.Key.Month),
                    catName,
                    amount);
            })
            .ToList();

        // Compute category totals for sorting
        var categoryTotals = grouped
            .GroupBy(g => g.CategoryName)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Amount));

        // Sort: by category total descending, then by year+month ascending within each category
        // Tie-break on category name ascending for stable ordering
        var sorted = grouped
            .OrderByDescending(g => categoryTotals[g.CategoryName])
            .ThenBy(g => g.CategoryName)
            .ThenBy(g => g.Year)
            .ThenBy(g => g.Month)
            .ToList();

        return sorted;
    }
}
