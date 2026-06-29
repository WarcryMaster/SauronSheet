namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Timeline Event DTO (REQ-009).
/// Chronological events: highest income, biggest expense, savings record,
/// first transaction, last transaction, etc.
/// Types: highest-income, biggest-expense, savings-record, first-transaction,
/// last-transaction, milestone.
/// </summary>
public record TimelineEventDto(
    string Type,
    string Label,
    string Description,
    string Date,
    decimal Amount,
    string Icon);
