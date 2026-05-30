namespace SauronSheet.Application.Features.Budgets.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DTOs;
using MediatR;
using Sentry;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Entities;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.Services;
using SauronSheet.Domain.Specifications;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Redesigned handler for GetBudgetVsActualQuery.
/// Uses BudgetCalculationService for budget metric calculation.
/// Accepts a date range parameter.
/// Includes categories without defined budgets with "Sin presupuesto" label.
/// Slice 5 — Budget redesign.
/// </summary>
public class GetBudgetVsActualQueryHandler
    : IRequestHandler<GetBudgetVsActualQuery, List<BudgetVsActualDto>>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly BudgetCalculationService _calcService;
    private readonly IUserContext _userContext;

    public GetBudgetVsActualQueryHandler(
        IBudgetRepository budgetRepo,
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo,
        BudgetCalculationService calcService,
        IUserContext userContext)
    {
        _budgetRepo = budgetRepo ?? throw new ArgumentNullException(nameof(budgetRepo));
        _transactionRepo = transactionRepo ?? throw new ArgumentNullException(nameof(transactionRepo));
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        _calcService = calcService ?? throw new ArgumentNullException(nameof(calcService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<List<BudgetVsActualDto>> Handle(
        GetBudgetVsActualQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = new UserId(_userContext.UserId);

            IReadOnlyList<Budget> budgets = await _budgetRepo.GetByUserIdAsync(userId);
            IReadOnlyList<Category> categories = await _categoryRepo.GetByUserIdAsync(userId);

            var categoryNameLookup = categories.ToDictionary(c => c.Id, c => c.Name.Value);
            var categoryColorLookup = categories.ToDictionary(c => c.Id, c => c.Color?.Value);

            // Get transactions in range, scoped to the current user (multi-tenant)
            var dateRangeSpec = new TransactionByDateRangeSpecification(
                request.From.ToDateTime(new TimeOnly(0, 0)),
                request.To.ToDateTime(new TimeOnly(23, 59, 59)));
            var userSpec = new TransactionByUserSpecification(userId);
            var scopedSpec = CompositeSpecification<Transaction>.And(dateRangeSpec, userSpec);
            IReadOnlyList<Transaction> transactions =
                await _transactionRepo.FindBySpecificationAsync(scopedSpec);

            // Group spending by category
            var spendingByCategory = transactions
                .Where(t => t.CategoryId != null)
                .GroupBy(t => t.CategoryId!)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount.Amount));

            var result = new List<BudgetVsActualDto>();
            var processedCategoryIds = new HashSet<CategoryId>();
            string currency = budgets.FirstOrDefault()?.Limit.Currency ?? "EUR";

            // Process categories with budgets
            foreach (Budget budget in budgets)
            {
                if (!IsActiveInRange(budget, request.From, request.To))
                    continue;

                processedCategoryIds.Add(budget.CategoryId);

                decimal spent = spendingByCategory.TryGetValue(budget.CategoryId, out decimal s)
                    ? s : 0m;

                var calcResult = _calcService.Calculate(
                    budget, request.From, request.To,
                    new Money(spent, budget.Limit.Currency));

                string categoryName = categoryNameLookup.TryGetValue(budget.CategoryId, out string? name)
                    ? name : budget.CategoryId.Value.ToString();

                string? categoryColor = categoryColorLookup.TryGetValue(budget.CategoryId, out string? color)
                    ? color : null;

                result.Add(new BudgetVsActualDto(
                    CategoryId: budget.CategoryId.Value,
                    CategoryName: categoryName,
                    CategoryColor: categoryColor,
                    BudgetLimit: calcResult.AccumulatedLimit.Amount,
                    ActualSpend: spent,
                    Difference: calcResult.Remaining.Amount,
                    PercentageUsed: calcResult.PercentageUsed,
                    StatusLevel: calcResult.StatusLevel.ToString(),
                    Currency: budget.Limit.Currency));
            }

            // Process categories with spending but no budget
            foreach (KeyValuePair<CategoryId, decimal> kvp in spendingByCategory)
            {
                if (processedCategoryIds.Contains(kvp.Key))
                    continue;

                string categoryName = categoryNameLookup.TryGetValue(kvp.Key, out string? cn)
                    ? cn : kvp.Key.Value.ToString();

                string? categoryColor = categoryColorLookup.TryGetValue(kvp.Key, out string? cc)
                    ? cc : null;

                result.Add(new BudgetVsActualDto(
                    CategoryId: kvp.Key.Value,
                    CategoryName: categoryName,
                    CategoryColor: categoryColor,
                    BudgetLimit: null,
                    ActualSpend: kvp.Value,
                    Difference: null,
                    PercentageUsed: null,
                    StatusLevel: "Sin presupuesto",
                    Currency: currency));
            }

            return result;
        }
        catch (DomainException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "GetBudgetVsActualQueryHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "A network error occurred. Please check your connection and try again.");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "GetBudgetVsActualQueryHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "An unexpected error occurred while comparing budget vs actual. Please try again.");
        }
    }

    private static bool IsActiveInRange(Budget budget, DateOnly from, DateOnly to)
    {
        DateOnly effectiveFrom = from > budget.EffectiveFrom ? from : budget.EffectiveFrom;
        DateOnly effectiveTo = budget.EffectiveUntil.HasValue &&
                               budget.EffectiveUntil.Value < to
            ? budget.EffectiveUntil.Value
            : to;
        return effectiveFrom <= effectiveTo;
    }
}
