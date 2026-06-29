namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Top Movement DTO (REQ-010).
/// Represents a single top transaction or frequent movement.
/// Types: income, expense, frequent.
/// TransactionId is null for frequent entries (aggregated).
/// </summary>
public record TopMovementDto(
    string Description,
    decimal Amount,
    string Date,
    string Category,
    string Type,
    string? TransactionId);
