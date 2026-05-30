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
/// Handler for GetBudgetMetricsQuery.
/// Calculates budget metrics for a date range, including categories without budgets.
/// Uses BudgetCalculationService for domain-level calculations.
/// Slice 5 — Budget redesign.
/// </summary>
public class GetBudgetMetricsQueryHandler
    : IRequestHandler<GetBudgetMetricsQuery, List<BudgetMetricsDto>>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly BudgetCalculationService _calcService;
    private readonly IUserContext _userContext;

    public GetBudgetMetricsQueryHandler(
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

    public async Task<List<BudgetMetricsDto>> Handle(
        GetBudgetMetricsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = new UserId(_userContext.UserId);

            // Load all data in parallel conceptually (sequentially for simplicity)
            IReadOnlyList<Budget> budgets = await _budgetRepo.GetByUserIdAsync(userId);
            IReadOnlyList<Category> categories = await _categoryRepo.GetByUserIdAsync(userId);

            var categoryNameLookup = categories.ToDictionary(c => c.Id, c => c.Name.Value);

            // Get transactions in the date range, scoped to the current user (multi-tenant)
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

            var result = new List<BudgetMetricsDto>();
            var processedCategoryIds = new HashSet<CategoryId>();

            // Calculate metrics for each budget active in the range
            foreach (Budget budget in budgets)
            {
                DateOnly budgetFrom = request.From;
                DateOnly budgetTo = request.To;
                BudgetDateRange? perBudgetRange = null;
                bool hasPerBudgetRange = request.PerBudgetDateRanges != null &&
                    request.PerBudgetDateRanges.TryGetValue(budget.Id.Value, out perBudgetRange);

                if (hasPerBudgetRange)
                {
                    budgetFrom = perBudgetRange!.From;
                    budgetTo = perBudgetRange.To;
                }

                if (!IsActiveInRange(budget, budgetFrom, budgetTo))
                    continue;

                processedCategoryIds.Add(budget.CategoryId);

                decimal spent;
                if (hasPerBudgetRange)
                {
                    // Filter spending to this budget's specific period range
                    spent = transactions
                        .Where(t => t.CategoryId == budget.CategoryId
                                 && DateOnly.FromDateTime(t.Date) >= budgetFrom
                                 && DateOnly.FromDateTime(t.Date) <= budgetTo)
                        .Sum(t => t.Amount.Amount);
                }
                else
                {
                    spent = spendingByCategory.TryGetValue(budget.CategoryId, out decimal s)
                        ? s
                        : 0m;
                }

                var calcResult = _calcService.Calculate(
                    budget,
                    budgetFrom,
                    budgetTo,
                    new Money(spent, budget.Limit.Currency));

                string categoryName = categoryNameLookup.TryGetValue(budget.CategoryId, out string? name)
                    ? name
                    : budget.CategoryId.Value.ToString();

                result.Add(new BudgetMetricsDto(
                    BudgetId: budget.Id.Value,
                    CategoryId: budget.CategoryId.Value,
                    CategoryName: categoryName,
                    PeriodsElapsed: calcResult.PeriodsElapsed,
                    AccumulatedLimit: calcResult.AccumulatedLimit.Amount,
                    Spent: calcResult.Spent.Amount,
                    Remaining: calcResult.Remaining.Amount,
                    PercentageUsed: calcResult.PercentageUsed,
                    StatusLevel: calcResult.StatusLevel.ToString()));
            }

            // Include categories with spending but no budget
            foreach (KeyValuePair<CategoryId, decimal> kvp in spendingByCategory)
            {
                if (processedCategoryIds.Contains(kvp.Key))
                    continue;

                string categoryName = categoryNameLookup.TryGetValue(kvp.Key, out string? name)
                    ? name
                    : kvp.Key.Value.ToString();

                result.Add(new BudgetMetricsDto(
                    BudgetId: Guid.Empty,
                    CategoryId: kvp.Key.Value,
                    CategoryName: categoryName,
                    PeriodsElapsed: 0,
                    AccumulatedLimit: 0m,
                    Spent: kvp.Value,
                    Remaining: -kvp.Value,
                    PercentageUsed: 0m,
                    StatusLevel: "Sin presupuesto"));
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
                scope.SetTag("handler", "GetBudgetMetricsQueryHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "A network error occurred. Please check your connection and try again.");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "GetBudgetMetricsQueryHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "An unexpected error occurred while calculating budget metrics. Please try again.");
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
