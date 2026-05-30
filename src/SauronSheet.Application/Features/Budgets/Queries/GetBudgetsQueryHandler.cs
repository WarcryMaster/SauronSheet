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
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Redesigned handler for GetBudgetsQuery.
/// Gets budgets for the current user, optionally filtering by AsOf date.
/// Resolves category names via ICategoryRepository.
/// Slice 5 — Budget redesign.
/// </summary>
public class GetBudgetsQueryHandler : IRequestHandler<GetBudgetsQuery, List<BudgetDto>>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetBudgetsQueryHandler(
        IBudgetRepository budgetRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _budgetRepo = budgetRepo ?? throw new ArgumentNullException(nameof(budgetRepo));
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<List<BudgetDto>> Handle(
        GetBudgetsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = new UserId(_userContext.UserId);

            IReadOnlyList<Domain.Entities.Budget> budgets =
                await _budgetRepo.GetByUserIdAsync(userId);

            if (request.AsOf.HasValue)
            {
                budgets = budgets
                    .Where(b => b.IsActiveOn(request.AsOf.Value))
                    .ToList();
            }

            // Resolve category names
            IReadOnlyList<Domain.Entities.Category> categories =
                await _categoryRepo.GetByUserIdAsync(userId);

            var categoryNameLookup = categories.ToDictionary(
                c => c.Id,
                c => c.Name.Value);

            return budgets
                .Select(b => new BudgetDto(
                    Id: b.Id.Value,
                    CategoryId: b.CategoryId.Value,
                    CategoryName: categoryNameLookup.TryGetValue(b.CategoryId, out string? name)
                        ? name
                        : b.CategoryId.Value.ToString(),
                    EffectiveFrom: b.EffectiveFrom,
                    EffectiveUntil: b.EffectiveUntil,
                    PeriodGranularity: b.PeriodGranularity.ToString(),
                    Limit: b.Limit.Amount))
                .ToList();
        }
        catch (DomainException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "GetBudgetsQueryHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "A network error occurred. Please check your connection and try again.");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("handler", "GetBudgetsQueryHandler");
                scope.Level = SentryLevel.Error;
            });
            throw new DomainException(
                "An unexpected error occurred while retrieving budgets. Please try again.");
        }
    }
}
