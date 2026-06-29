namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Prediction DTO (REQ-016).
/// Deterministic linear regression projections. Confidence is R² in [0,1].
/// </summary>
public record PredictionDto(
    decimal? ProjectedIncome,
    decimal? ProjectedExpense,
    decimal? ProjectedSavings,
    decimal? ProjectedBalance,
    decimal? Confidence,
    int YearsRequired,
    bool HasEnoughData,
    string Message);
