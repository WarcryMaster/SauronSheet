namespace SauronSheet.Application.Features.Analytics.Queries;

using System;
using System.Collections.Generic;
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
/// Handler for GetSpendingByCategoryQuery.
/// Groups expenses by category with percentages. Groups &gt;6 categories into "Other".
/// Phase 4 (US2).
/// </summary>
public class GetSpendingByCategoryQueryHandler
    : IRequestHandler<GetSpendingByCategoryQuery, List<CategorySpendingDto>>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetSpendingByCategoryQueryHandler(
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
        _userContext = userContext;
    }

    public async Task<List<CategorySpendingDto>> Handle(
        GetSpendingByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(_userContext.UserId);

        var userSpec = new TransactionByUserSpecification(userId);
        var dateSpec = new TransactionByDateRangeSpecification(request.FromDate, request.ToDate);
        var composedSpec = CompositeSpecification<Domain.Entities.Transaction>.And(userSpec, dateSpec);

        var transactions = await _transactionRepo.FindBySpecificationAsync(composedSpec);

        // Only expenses (negative amounts)
        var expenses = transactions.Where(t => t.Amount.IsNegative).ToList();
        if (!expenses.Any())
            return new List<CategorySpendingDto>();

        var totalSpending = expenses.Sum(t => Math.Abs(t.Amount.Amount));

        // Load categories for name/color lookup
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c);

        // Group by category
        var grouped = expenses
            .GroupBy(t => t.CategoryId)
            .Select(g =>
            {
                var amount = g.Sum(t => Math.Abs(t.Amount.Amount));
                var catId = g.Key;
                var catName = "Uncategorized";
                string? catColor = null;

                if (catId != null && categoryLookup.TryGetValue(catId, out var cat))
                {
                    catName = cat.Name.Value;
                    catColor = cat.Color.Value;
                }

                return new CategorySpendingDto(
                    catId?.Value,
                    catName,
                    catColor,
                    amount,
                    "EUR",
                    totalSpending > 0 ? Math.Round(amount / totalSpending * 100, 2) : 0);
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        // Group into "Other" if more than 6 categories
        if (grouped.Count > 6)
        {
            var top6 = grouped.Take(6).ToList();
            var others = grouped.Skip(6).ToList();
            var otherAmount = others.Sum(c => c.Amount);
            var otherPercentage = totalSpending > 0 ? Math.Round(otherAmount / totalSpending * 100, 2) : 0;

            top6.Add(new CategorySpendingDto(
                null,
                "Other",
                "#6B7280",
                otherAmount,
                "EUR",
                otherPercentage));

            return top6;
        }

        return grouped;
    }
}
