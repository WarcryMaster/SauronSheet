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

/// <summary>
/// Handler for GetMonthlySpendingByCategoryQuery.
/// Returns monthly spending broken down by category for stacked area charts.
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
        var dateSpec = new TransactionByDateRangeSpecification(
            new DateTime(request.Year, 1, 1),
            new DateTime(request.Year, 12, 31, 23, 59, 59));
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);

        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        // Only expenses
        var expenses = transactions.Where(t => t.Amount.IsNegative).ToList();
        if (!expenses.Any())
            return new List<MonthlyCategorySpendingDto>();

        // Load categories for name lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c);

        // Group by month and category
        var grouped = expenses
            .GroupBy(t => new { t.Date.Month, CategoryId = t.CategoryId })
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
                    g.Key.Month,
                    CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(g.Key.Month),
                    catName,
                    amount);
            })
            .ToList();

        return grouped;
    }
}
