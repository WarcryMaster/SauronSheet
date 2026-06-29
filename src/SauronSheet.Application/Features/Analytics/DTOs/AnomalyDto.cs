namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Anomaly DTO (REQ-008).
/// Type values: anomaly, extraordinary, exceptional.
/// </summary>
public record AnomalyDto(
    string Category,
    int Month,
    decimal Amount,
    decimal Mean,
    decimal StandardDeviation,
    string Type,
    string Description);
