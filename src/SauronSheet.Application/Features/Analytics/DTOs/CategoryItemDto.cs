namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Category Distribution item DTO (REQ-005, REQ-006).
/// Represents a single category's aggregate data: amount, percentage share,
/// ranking, YoY change, trend direction, and whether it's new this year.
/// </summary>
public record CategoryItemDto(
    string CategoryName,
    decimal Amount,
    decimal Percentage,
    int Rank,
    decimal? YoYChangeAbs,
    decimal? YoYChangePct,
    string Trend,
    bool IsNewThisYear);
