using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Budgets.DTOs;
using SauronSheet.Application.Features.Budgets.Queries;
using SauronSheet.Application.Features.Budgets.Commands;
using SauronSheet.Domain.Exceptions;
using SauronSheet.Domain.ValueObjects;

namespace SauronSheet.Frontend.Pages.Budgets;

[Authorize]
[ValidateAntiForgeryToken]
public class EditModel : PageModel
{
    private readonly IMediator _mediator;

    public BudgetDto? Budget { get; set; }

    [BindProperty]
    public Guid BudgetId { get; set; }

    [BindProperty]
    public decimal NewLimitAmount { get; set; }

    [BindProperty]
    public string PeriodGranularity { get; set; } = nameof(BudgetPeriod.Monthly);

    [BindProperty]
    public DateOnly EffectiveFrom { get; set; }

    [BindProperty]
    public DateOnly? EffectiveUntil { get; set; }

    public string? ErrorMessage { get; set; }

    public EditModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            IReadOnlyList<BudgetDto> budgets = await _mediator.Send(new GetBudgetsQuery());
            Budget = budgets.FirstOrDefault(b => b.Id == id);

            if (Budget == null)
            {
                return RedirectToPage("/Budgets/Index");
            }

            BudgetId = Budget.Id;
            NewLimitAmount = Budget.Limit;
            PeriodGranularity = Budget.PeriodGranularity;
            EffectiveFrom = Budget.EffectiveFrom;
            EffectiveUntil = Budget.EffectiveUntil;

            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        try
        {
            // Validate limit
            if (NewLimitAmount <= 0)
            {
                ErrorMessage = "Spending limit must be greater than zero.";
                await ReloadBudgetAsync();
                return Page();
            }

            if (!Enum.TryParse<BudgetPeriod>(PeriodGranularity, ignoreCase: true, out BudgetPeriod granularity))
            {
                ErrorMessage = "Invalid period granularity.";
                await ReloadBudgetAsync();
                return Page();
            }

            bool limitChanged = NewLimitAmount != Budget?.Limit;
            bool periodChanged = PeriodGranularity != Budget?.PeriodGranularity;
            bool effectiveFromChanged = EffectiveFrom != Budget?.EffectiveFrom;
            bool effectiveUntilChanged = EffectiveUntil != Budget?.EffectiveUntil;

            // Apply date changes first (if any)
            if (effectiveFromChanged || effectiveUntilChanged)
            {
                await _mediator.Send(new UpdateBudgetEffectiveDatesCommand(
                    BudgetId, EffectiveFrom, EffectiveUntil));
            }

            if (periodChanged)
            {
                await _mediator.Send(new UpdateBudgetPeriodCommand(
                    BudgetId, granularity, NewLimitAmount));
            }
            else if (limitChanged)
            {
                await _mediator.Send(new UpdateBudgetLimitCommand(
                    BudgetId, NewLimitAmount));
            }

            TempData["SuccessMessage"] = "Budget updated successfully.";
            return RedirectToPage("/Budgets/Index");
        }
        catch (DomainException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Edit.OnPostUpdateAsync");
                scope.SetTag("exception_type", "DomainException");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            ErrorMessage = ex.Message;
            await ReloadBudgetAsync();
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
        catch (HttpRequestException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Edit.OnPostUpdateAsync");
                scope.SetTag("exception_type", "HttpRequestException");
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "A network error occurred. Please check your connection and try again.";
            await ReloadBudgetAsync();
            return Page();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Edit.OnPostUpdateAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "An unexpected error occurred while updating the budget. Please try again later.";
            await ReloadBudgetAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostStatusAsync()
    {
        try
        {
            IReadOnlyList<BudgetDto> budgets = await _mediator.Send(new GetBudgetsQuery());
            BudgetDto? budget = budgets.FirstOrDefault(b => b.Id == BudgetId);

            if (budget is null)
            {
                throw new DomainException("Budget not found.");
            }

            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            bool isActive = budget.EffectiveUntil == null || budget.EffectiveUntil.Value >= today;

            if (isActive)
            {
                DateOnly yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
                await _mediator.Send(new DeactivateBudgetCommand(BudgetId, yesterday));
                TempData["SuccessMessage"] = "Budget deactivated successfully.";
            }
            else
            {
                await _mediator.Send(new UpdateBudgetEffectiveDatesCommand(
                    BudgetId,
                    budget.EffectiveFrom,
                    null));
                TempData["SuccessMessage"] = "Budget activated successfully.";
            }

            return RedirectToPage("/Budgets/Index");
        }
        catch (DomainException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Edit.OnPostStatusAsync");
                scope.SetTag("exception_type", "DomainException");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            ErrorMessage = ex.Message;
            await ReloadBudgetAsync();
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
        catch (HttpRequestException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Edit.OnPostStatusAsync");
                scope.SetTag("exception_type", "HttpRequestException");
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "A network error occurred. Please check your connection and try again.";
            await ReloadBudgetAsync();
            return Page();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Edit.OnPostStatusAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "An unexpected error occurred while updating the budget status. Please try again later.";
            await ReloadBudgetAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        try
        {
            await _mediator.Send(new DeleteBudgetCommand(BudgetId));
            TempData["SuccessMessage"] = "Budget deleted permanently.";
            return RedirectToPage("/Budgets/Index");
        }
        catch (DomainException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Edit.OnPostDeleteAsync");
                scope.SetTag("exception_type", "DomainException");
                scope.Level = Sentry.SentryLevel.Warning;
            });
            ErrorMessage = ex.Message;
            await ReloadBudgetAsync();
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage("/auth/login");
        }
        catch (HttpRequestException ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Edit.OnPostDeleteAsync");
                scope.SetTag("exception_type", "HttpRequestException");
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "A network error occurred. Please check your connection and try again.";
            await ReloadBudgetAsync();
            return Page();
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("page", "Budgets/Edit.OnPostDeleteAsync");
                scope.SetTag("exception_type", ex.GetType().Name);
                scope.Level = Sentry.SentryLevel.Error;
            });
            ErrorMessage = "An unexpected error occurred while deleting the budget. Please try again later.";
            await ReloadBudgetAsync();
            return Page();
        }
    }

    private async Task ReloadBudgetAsync()
    {
        try
        {
            IReadOnlyList<BudgetDto> budgets = await _mediator.Send(new GetBudgetsQuery());
            Budget = budgets.FirstOrDefault(b => b.Id == BudgetId);
            if (Budget != null)
            {
                NewLimitAmount = Budget.Limit;
                PeriodGranularity = Budget.PeriodGranularity;
                EffectiveFrom = Budget.EffectiveFrom;
                EffectiveUntil = Budget.EffectiveUntil;
            }
        }
        catch
        {
            Budget = null;
        }
    }
}
