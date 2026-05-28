namespace SauronSheet.Application.Features.Transactions.Queries;

using System;
using MediatR;

/// <summary>
/// Query to get the current user's transaction date range.
/// Returns null when the user has no transactions.
/// </summary>
public record GetTransactionDateRangeQuery() : IRequest<(DateTime MinDate, DateTime MaxDate)?>;
