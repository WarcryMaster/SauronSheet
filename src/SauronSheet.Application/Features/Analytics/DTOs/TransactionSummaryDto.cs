namespace SauronSheet.Application.Features.Analytics.DTOs;

/// <summary>
/// Summary statistics for transactions within a date range.
/// Phase 4 (US6): Transaction summary cards on dashboard.
/// </summary>
public record TransactionSummaryDto(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetAmount,
    int TransactionCount,
    string Currency,
    DateTime FromDate,
    DateTime ToDate);
