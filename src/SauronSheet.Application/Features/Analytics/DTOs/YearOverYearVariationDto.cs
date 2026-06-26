namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Year-over-year percentage variation for each summary field.
/// All percentage fields are nullable — null means zero-division or no previous data.
/// </summary>
public record YearOverYearVariationDto(
    decimal? IncomeFixedPct,
    decimal? IncomeVariablePct,
    decimal? IncomeTotalPct,
    decimal? ExpenseFixedPct,
    decimal? ExpenseVariablePct,
    decimal? ExpenseTotalPct,
    decimal? NetPct,
    bool HasPreviousYearData);
