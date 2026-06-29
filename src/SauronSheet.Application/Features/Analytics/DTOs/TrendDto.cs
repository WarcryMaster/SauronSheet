namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Trend classification DTO (REQ-015).
/// Direction values: growing, stable, declining, insufficient.
/// </summary>
public record TrendDto(
    string Category,
    string Direction,
    decimal? ChangePercentage,
    string Icon);
