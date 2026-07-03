namespace SauronSheet.Application.Features.Budgets.Queries;

using System;
using System.Collections.Generic;
using System.Globalization;
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
/// Handler for GetBudgetHistoryQuery.
/// Breaks the year into periods based on each budget's granularity
/// and calculates per-period accumulated metrics.
/// Slice 5 — Budget redesign.
/// </summary>
public class GetBudgetHistoryQueryHandler
    : IRequestHandler<GetBudgetHistoryQuery, List<BudgetPeriodSummaryDto>>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly BudgetCalculationService _calcService;
    private readonly IUserContext _userContext;

    private static readonly string[] MonthNames =
    {
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };

    public GetBudgetHistoryQueryHandler(
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

    public async Task<List<BudgetPeriodSummaryDto>> Handle(
        GetBudgetHistoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = new UserId(_userContext.UserId);
            var yearStart = new DateOnly(request.Year, 1, 1);
            var yearEnd = new DateOnly(request.Year, 12, 31);

            IReadOnlyList<Budget> budgets = await _budgetRepo.GetByUserIdAsync(userId);

            // Get all transactions in the year, scoped to the current user (multi-tenant)
            var dateRangeSpec = new TransactionByDateRangeSpecification(
                yearStart.ToDateTime(new TimeOnly(0, 0)),
                yearEnd.ToDateTime(new TimeOnly(23, 59, 59)));
            var userSpec = new TransactionByUserSpecification(userId);
            var scopedSpec = CompositeSpecification<Transaction>.And(dateRangeSpec, userSpec);
            IReadOnlyList<Transaction> transactions =
                await _transactionRepo.FindBySpecificationAsync(scopedSpec);

            var result = new List<BudgetPeriodSummaryDto>();

            foreach (Budget budget in budgets)
            {
                if (!IsActiveInYear(budget, request.Year))
                    continue;

                // Generate periods based on granularity
                List<(DateOnly From, DateOnly To, string Label)> periods =
                    GetPeriodsForYear(budget.PeriodGranularity, request.Year);

                int accumulatedPeriods = 0;
                Money accumulatedLimit = new Money(0m, budget.Limit.Currency);
                decimal spentSoFar = 0m;

                // Count periods before the year (if budget started before this year)
                if (budget.EffectiveFrom < yearStart)
                {
                    DateOnly preYearStart = maxDate(budget.EffectiveFrom, yearStart);
                    // Calculate periods from EffectiveFrom to yearStart
                    DateOnly preYearEnd = yearStart.AddDays(-1);
                    accumulatedPeriods = _calcService.PeriodsElapsed(
                        budget.PeriodGranularity, budget.EffectiveFrom, preYearEnd);
                    accumulatedLimit = new Money(
                        budget.Limit.Amount * accumulatedPeriods, budget.Limit.Currency);
                }

                foreach ((DateOnly from, DateOnly to, string label) in periods)
                {
                    DateOnly periodFrom = maxDate(from, budget.EffectiveFrom);
                    DateOnly periodTo = budget.EffectiveUntil.HasValue
                        ? minDate(to, budget.EffectiveUntil.Value)
                        : to;

                    if (periodFrom > periodTo)
                        continue;

                    accumulatedPeriods++;
                    accumulatedLimit = accumulatedLimit.Plus(
                        new Money(budget.Limit.Amount, budget.Limit.Currency));

                    // Sum spending for this period and category (expenses are stored as negative, use absolute for budget comparison)
                    decimal periodSpent = Math.Abs(transactions
                        .Where(t => t.CategoryId == budget.CategoryId
                            && DateOnly.FromDateTime(t.Date) >= periodFrom
                            && DateOnly.FromDateTime(t.Date) <= periodTo)
                        .Sum(t => t.Amount.Amount));

                    spentSoFar += periodSpent;

                    decimal percentageUsed = accumulatedLimit.IsZero
                        ? 0m
                        : Math.Round(spentSoFar / accumulatedLimit.Amount * 100m, 2);

                    BudgetStatusLevel statusLevel = percentageUsed < 75m ? BudgetStatusLevel.Green
                        : percentageUsed < 100m ? BudgetStatusLevel.Yellow
                        : percentageUsed == 100m ? BudgetStatusLevel.Red
                        : BudgetStatusLevel.Overage;

                    Money remaining = accumulatedLimit.Minus(
                        new Money(spentSoFar, budget.Limit.Currency));

                    result.Add(new BudgetPeriodSummaryDto(
                        Period: label,
                        AccumulatedLimit: accumulatedLimit.Amount,
                        Spent: spentSoFar,
                        Remaining: remaining.Amount,
                        StatusLevel: statusLevel.ToString()));
                }
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
                scope.SetTag("handler", "GetBudgetHistoryQueryHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "A network error occurred. Please check your connection and try again.");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "GetBudgetHistoryQueryHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "An unexpected error occurred while retrieving budget history. Please try again.");
        }
    }

    private static bool IsActiveInYear(Budget budget, int year)
    {
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);

        if (budget.EffectiveFrom > yearEnd)
            return false;
        if (budget.EffectiveUntil.HasValue && budget.EffectiveUntil.Value < yearStart)
            return false;

        return true;
    }

    private static List<(DateOnly From, DateOnly To, string Label)> GetPeriodsForYear(
        BudgetPeriod granularity, int year)
    {
        return granularity switch
        {
            BudgetPeriod.Monthly => Enumerable.Range(1, 12)
                .Select(m => (
                    From: new DateOnly(year, m, 1),
                    To: new DateOnly(year, m, DateTime.DaysInMonth(year, m)),
                    Label: MonthNames[m - 1]
                )).ToList(),

            BudgetPeriod.Quarterly => new List<(DateOnly, DateOnly, string)>
            {
                (new DateOnly(year, 1, 1), new DateOnly(year, 3, 31), $"Q1 {year}"),
                (new DateOnly(year, 4, 1), new DateOnly(year, 6, 30), $"Q2 {year}"),
                (new DateOnly(year, 7, 1), new DateOnly(year, 9, 30), $"Q3 {year}"),
                (new DateOnly(year, 10, 1), new DateOnly(year, 12, 31), $"Q4 {year}"),
            },

            BudgetPeriod.Semester => new List<(DateOnly, DateOnly, string)>
            {
                (new DateOnly(year, 1, 1), new DateOnly(year, 6, 30), $"H1 {year}"),
                (new DateOnly(year, 7, 1), new DateOnly(year, 12, 31), $"H2 {year}"),
            },

            BudgetPeriod.Annual => new List<(DateOnly, DateOnly, string)>
            {
                (new DateOnly(year, 1, 1), new DateOnly(year, 12, 31), $"{year}"),
            },

            _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity,
                "Unknown budget period granularity.")
        };
    }

    private static DateOnly maxDate(DateOnly a, DateOnly b) => a > b ? a : b;
    private static DateOnly minDate(DateOnly a, DateOnly b) => a < b ? a : b;
}
