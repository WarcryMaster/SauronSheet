namespace SauronSheet.Application.Features.Analytics.Queries;

using DTOs;
using MediatR;

/// <summary>
/// Query to get transaction summary statistics for a date range.
/// Phase 4 (US6).
/// </summary>
public record GetTransactionSummaryQuery(DateTime FromDate, DateTime ToDate) : IRequest<TransactionSummaryDto>;
