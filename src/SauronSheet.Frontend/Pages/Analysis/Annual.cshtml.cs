using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediatR;
using SauronSheet.Application.Features.Analytics.DTOs;
using SauronSheet.Application.Features.Analytics.Queries;
using SauronSheet.Domain.Exceptions;

namespace SauronSheet.Frontend.Pages.Analysis;

/// <summary>
/// Annual fixed vs variable analysis page model.
/// Renders a breakdown of income and expenses by month for the selected year.
/// </summary>
[Authorize]
public class AnnualModel : PageModel
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Selected year for the analysis. Defaults to the current UTC year.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    /// <summary>
    /// Analysis result returned by the query handler.
    /// </summary>
    public AnnualAnalysisResultDto? Result { get; set; }

    /// <summary>
    /// Convenience flag indicating whether the analysis contains data.
    /// </summary>
    public bool HasData => Result?.HasData ?? false;

    /// <summary>
    /// Rows filtered to income only (IsIncome == true).
    /// </summary>
    public IReadOnlyList<AnnualAnalysisRowDto> IncomeRows =>
        Result?.Rows.Where(r => r.IsIncome).ToArray()
        ?? Array.Empty<AnnualAnalysisRowDto>();

    /// <summary>
    /// Rows filtered to expenses only (IsIncome == false).
    /// </summary>
    public IReadOnlyList<AnnualAnalysisRowDto> ExpenseRows =>
        Result?.Rows.Where(r => !r.IsIncome).ToArray()
        ?? Array.Empty<AnnualAnalysisRowDto>();

    /// <summary>
    /// User-facing error message for domain rule violations.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    public AnnualModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        int selectedYear = Year ?? DateTime.UtcNow.Year;

        try
        {
            Result = await _mediator.Send(
                new GetAnnualAnalysisQuery(selectedYear),
                cancellationToken);

            return Page();
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage("/Error");
        }
        catch (Exception ex)
        {
            Sentry.SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetTag("analysis", "annual");
                scope.SetTag("year", selectedYear.ToString(CultureInfo.InvariantCulture));
                scope.Level = Sentry.SentryLevel.Error;
            });

            return RedirectToPage("/Error");
        }
    }

    /// <summary>
    /// Formats a YoY variation percentage for display.
    /// Returns "N/A" when null, or "+XX.X%"/"-XX.X%" otherwise.
    /// </summary>
    public static string FormatVariationPct(decimal? pct)
    {
        if (pct is null)
        {
            return "N/A";
        }

        string sign = pct.Value >= 0 ? "+" : string.Empty;
        return $"{sign}{pct.Value.ToString("F1", CultureInfo.InvariantCulture)}%";
    }

    /// <summary>
    /// Returns the MDB badge class for the variation direction.
    /// Income: up=success (green), down=danger (red)
    /// Expense: up=danger (red), down=success (green)
    /// Net: up=success (green), down=danger (red)
    /// </summary>
    public static string GetVariationBadgeClass(decimal? pct, bool isIncomeOrNet)
    {
        if (pct is null || pct.Value == 0m)
        {
            return "bg-secondary";
        }

        if (isIncomeOrNet)
        {
            return pct.Value > 0 ? "bg-success" : "bg-danger";
        }

        // Expense: up is bad (red), down is good (green)
        return pct.Value > 0 ? "bg-danger" : "bg-success";
    }

    /// <summary>
    /// Returns the arrow symbol for the variation.
    /// Positive → up arrow, Negative → down arrow.
    /// </summary>
    public static string GetVariationArrow(decimal? pct)
    {
        if (pct is null || pct.Value == 0m)
        {
            return string.Empty;
        }

        return pct.Value > 0 ? "\u2191" : "\u2193";
    }
}
