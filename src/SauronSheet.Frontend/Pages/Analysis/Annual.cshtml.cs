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
                scope.SetTag("year", selectedYear.ToString(System.Globalization.CultureInfo.InvariantCulture));
                scope.Level = Sentry.SentryLevel.Error;
            });

            return RedirectToPage("/Error");
        }
    }
}
