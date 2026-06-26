namespace SauronSheet.Application.Features.Budgets.Queries;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DTOs;
using MediatR;
using SauronSheet.Domain.Common;
using SauronSheet.Domain.Repositories;
using SauronSheet.Domain.ValueObjects;

/// <summary>
/// Handler for GetBudgetByIdQuery.
/// Fetches a single budget by ID for the current user and resolves the category name.
/// </summary>
public class GetBudgetByIdQueryHandler : IRequestHandler<GetBudgetByIdQuery, BudgetDto?>
{
    private readonly IBudgetRepository _budgetRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserContext _userContext;

    public GetBudgetByIdQueryHandler(
        IBudgetRepository budgetRepo,
        ICategoryRepository categoryRepo,
        IUserContext userContext)
    {
        _budgetRepo = budgetRepo ?? throw new ArgumentNullException(nameof(budgetRepo));
        _categoryRepo = categoryRepo ?? throw new ArgumentNullException(nameof(categoryRepo));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<BudgetDto?> Handle(
        GetBudgetByIdQuery request, CancellationToken cancellationToken)
    {
        var budgetId = new BudgetId(request.BudgetId);
        var budget = await _budgetRepo.GetByIdAsync(budgetId);

        if (budget is null)
            return null;

        // Resolve category name
        var userId = new UserId(_userContext.UserId);
        var categories = await _categoryRepo.GetByUserIdAsync(userId);
        var categoryName = categories
            .Where(c => c.Id == budget.CategoryId)
            .Select(c => c.Name.Value)
            .FirstOrDefault() ?? budget.CategoryId.Value.ToString();

        return new BudgetDto(
            Id: budget.Id.Value,
            CategoryId: budget.CategoryId.Value,
            CategoryName: categoryName,
            EffectiveFrom: budget.EffectiveFrom,
            EffectiveUntil: budget.EffectiveUntil,
            PeriodGranularity: budget.PeriodGranularity.ToString(),
            Limit: budget.Limit.Amount);
    }
}
